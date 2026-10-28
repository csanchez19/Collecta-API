using Collecta.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Collecta.API.Features.Catalog;

/// <summary>Sets, their items and price history. Public: the user id only adds the caller's own numbers.</summary>
public sealed class CatalogService(AppDbContext db)
{
    /// <summary>How far back the item sheet compares the price to.</summary>
    private const int PriceChangeDays = 30;

    /// <returns>Null when the collection does not exist.</returns>
    public async Task<IReadOnlyList<SetResponse>?> ListSetsAsync(Guid collectionId, Guid? userId, CancellationToken ct)
    {
        if (!await db.Collections.AnyAsync(c => c.Id == collectionId, ct))
        {
            return null;
        }

        // Newest first, like every TCG app lists expansions.
        var sets = db.CollectionSets
            .Where(s => s.CollectionId == collectionId)
            .OrderByDescending(s => s.ReleaseDate)
            .ThenBy(s => s.Name);
        return await ProjectSets(sets, userId ?? Guid.Empty).ToListAsync(ct);
    }

    public Task<SetResponse?> GetSetAsync(Guid setId, Guid? userId, CancellationToken ct) =>
        ProjectSets(db.CollectionSets.Where(s => s.Id == setId), userId ?? Guid.Empty).SingleOrDefaultAsync(ct);

    /// <returns>Every item of the set in album order (sets hold a few hundred at most), or null when the set does not exist.</returns>
    public async Task<IReadOnlyList<ItemResponse>?> ListItemsAsync(Guid setId, Guid? userId, CancellationToken ct)
    {
        if (!await db.CollectionSets.AnyAsync(s => s.Id == setId, ct))
        {
            return null;
        }

        var rows = await db.CatalogItems
            .Where(i => i.SetId == setId)
            .InAlbumOrder()
            .ForUser(db, userId ?? Guid.Empty)
            .ToListAsync(ct);
        return rows.ConvertAll(row => row.ToResponse());
    }

    public async Task<ItemResponse?> GetItemAsync(Guid itemId, Guid? userId, CancellationToken ct)
    {
        var row = await db.CatalogItems
            .Where(i => i.Id == itemId)
            .ForUser(db, userId ?? Guid.Empty)
            .SingleOrDefaultAsync(ct);
        return row?.ToResponse(await PriceChangeAsync(itemId, row.MarketValueCents, ct));
    }

    /// <returns>Daily prices inside the window, oldest first; null when the item does not exist.</returns>
    public async Task<IReadOnlyList<PricePointResponse>?> GetPriceHistoryAsync(Guid itemId, int days, CancellationToken ct)
    {
        if (!await db.CatalogItems.AnyAsync(i => i.Id == itemId, ct))
        {
            return null;
        }

        var from = Today().AddDays(-days);
        return await db.PriceSnapshots
            .Where(p => p.CatalogItemId == itemId && p.Date >= from)
            .OrderBy(p => p.Date)
            .Select(p => new PricePointResponse(p.Date, p.ValueCents))
            .ToListAsync(ct);
    }

    /// <summary>% change from the snapshot closest to 30 days ago (or the oldest one) to the current price.</summary>
    private async Task<decimal?> PriceChangeAsync(Guid itemId, long? currentCents, CancellationToken ct)
    {
        if (currentCents is not > 0)
        {
            return null;
        }

        var today = Today();
        var reference = Today().AddDays(-PriceChangeDays);
        var history = db.PriceSnapshots.Where(p => p.CatalogItemId == itemId && p.Date < today);
        var past = await history.Where(p => p.Date <= reference).OrderByDescending(p => p.Date).Select(p => (long?)p.ValueCents).FirstOrDefaultAsync(ct)
            ?? await history.OrderBy(p => p.Date).Select(p => (long?)p.ValueCents).FirstOrDefaultAsync(ct);

        return past is > 0
            ? Math.Round((currentCents.Value - past.Value) * 100m / past.Value, 1)
            : null;
    }

    private IQueryable<SetResponse> ProjectSets(IQueryable<Domain.CollectionSet> sets, Guid userId) =>
        sets.Select(s => new SetResponse(
            s.Id,
            s.CollectionId,
            s.Name,
            s.Series,
            s.ReleaseDate,
            s.LogoUrl,
            s.SymbolUrl,
            s.Items.Count,
            db.UserItems.Count(ui => ui.UserId == userId && ui.CatalogItem.SetId == s.Id),
            db.WishlistItems.Count(w => w.UserId == userId && w.CatalogItem.SetId == s.Id
                && !db.UserItems.Any(ui => ui.UserId == userId && ui.CatalogItemId == w.CatalogItemId)),
            db.UserItems
                .Where(ui => ui.UserId == userId && ui.CatalogItem.SetId == s.Id)
                .Sum(ui => ui.Quantity * (ui.CatalogItem.MarketValueCents ?? 0))));

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);
}

using Collecta.API.Domain;
using Collecta.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Collecta.API.Features.Collections;

/// <summary>Collections with the caller's progress. Public: an anonymous caller gets zeros.</summary>
public sealed class CollectionsService(AppDbContext db)
{
    public async Task<IReadOnlyList<CollectionResponse>> ListAsync(Guid? userId, CancellationToken ct)
    {
        var totalUsers = await db.Users.CountAsync(ct);
        var rows = await ProjectForUser(db.Collections.OrderBy(c => c.Name), userId ?? Guid.Empty).ToListAsync(ct);
        return rows.ConvertAll(r => r.ToResponse(totalUsers));
    }

    public async Task<CollectionResponse?> GetAsync(Guid collectionId, Guid? userId, CancellationToken ct)
    {
        var row = await ProjectForUser(db.Collections.Where(c => c.Id == collectionId), userId ?? Guid.Empty)
            .SingleOrDefaultAsync(ct);
        return row?.ToResponse(await db.Users.CountAsync(ct));
    }

    // Filter and sort on the entity before calling this: EF cannot translate operators applied
    // after a projection into a positional record. Guid.Empty (anonymous) matches no user rows.
    private IQueryable<CollectionRow> ProjectForUser(IQueryable<Collection> source, Guid userId) =>
        source.Select(c => new CollectionRow(
            c.Id,
            c.Name,
            c.Kind,
            c.KindLabel,
            c.AccentHue,
            // Items hang from the collection's sets.
            c.Sets.Sum(s => s.Items.Count),
            c.Sets.Count,
            db.UserItems.Count(ui => ui.UserId == userId && ui.CatalogItem.Set.CollectionId == c.Id),
            db.UserItems
                .Where(ui => ui.UserId == userId && ui.CatalogItem.Set.CollectionId == c.Id)
                .Sum(ui => ui.Quantity * (ui.CatalogItem.MarketValueCents ?? 0)),
            db.UserItems
                .Where(ui => ui.CatalogItem.Set.CollectionId == c.Id)
                .Select(ui => ui.UserId)
                .Distinct()
                .Count(),
            db.FavoriteCollections.Any(f => f.UserId == userId && f.CollectionId == c.Id)));

    private sealed record CollectionRow(
        Guid Id,
        string Name,
        CollectionKind Kind,
        string KindLabel,
        int AccentHue,
        int TotalCount,
        int SetCount,
        int OwnedCount,
        long OwnedValueCents,
        int CollectorCount,
        bool IsFavorite)
    {
        public CollectionResponse ToResponse(int totalUsers) => new(
            Id,
            Name,
            Kind,
            KindLabel,
            AccentHue,
            TotalCount,
            SetCount,
            OwnedCount,
            OwnedValueCents,
            Mine: OwnedCount > 0,
            IsFavorite,
            Popularity: totalUsers == 0 ? 0 : (int)Math.Round(100.0 * CollectorCount / totalUsers));
    }
}

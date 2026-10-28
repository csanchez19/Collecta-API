using Collecta.API.Domain;
using Collecta.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Collecta.API.Features.Collections;

public sealed class CollectionsService(AppDbContext db)
{
    public async Task<IReadOnlyList<CollectionResponse>> ListAsync(Guid userId, CancellationToken ct)
    {
        var totalUsers = await db.Users.CountAsync(ct);
        var rows = await ProjectForUser(db.Collections.OrderBy(c => c.Name), userId).ToListAsync(ct);
        return rows.ConvertAll(r => r.ToResponse(totalUsers));
    }

    public async Task<CollectionResponse?> GetAsync(Guid collectionId, Guid userId, CancellationToken ct)
    {
        var row = await ProjectForUser(db.Collections.Where(c => c.Id == collectionId), userId)
            .SingleOrDefaultAsync(ct);
        return row?.ToResponse(await db.Users.CountAsync(ct));
    }

    public async Task<IReadOnlyList<CatalogItemResponse>?> ListItemsAsync(Guid collectionId, Guid userId, CancellationToken ct)
    {
        if (!await db.Collections.AnyAsync(c => c.Id == collectionId, ct))
        {
            return null;
        }

        return await db.CatalogItems
            .Where(i => i.Set.CollectionId == collectionId)
            .OrderBy(i => i.Code).ThenBy(i => i.Name)
            .Select(i => new CatalogItemResponse(
                i.Id,
                i.Name,
                i.Code,
                i.ImageUrl,
                i.MarketValueCents,
                db.UserItems
                    .Where(ui => ui.UserId == userId && ui.CatalogItemId == i.Id)
                    .Select(ui => ui.Quantity)
                    .FirstOrDefault()))
            .ToListAsync(ct);
    }

    /// <returns>False when the collection does not exist.</returns>
    public async Task<bool> SetFavoriteAsync(Guid collectionId, Guid userId, bool favorite, CancellationToken ct)
    {
        if (!await db.Collections.AnyAsync(c => c.Id == collectionId, ct))
        {
            return false;
        }

        var existing = await db.FavoriteCollections.FindAsync([userId, collectionId], ct);
        if (favorite && existing is null)
        {
            db.FavoriteCollections.Add(new FavoriteCollection { UserId = userId, CollectionId = collectionId });
        }
        else if (!favorite && existing is not null)
        {
            db.FavoriteCollections.Remove(existing);
        }

        await db.SaveChangesAsync(ct);
        return true;
    }

    // Filter and sort on the entity before calling this: EF cannot translate operators applied
    // after a projection into a positional record.
    private IQueryable<CollectionRow> ProjectForUser(IQueryable<Collection> source, Guid userId) =>
        source.Select(c => new CollectionRow(
            c.Id,
            c.Name,
            c.Kind,
            c.KindLabel,
            c.AccentHue,
            // Items hang from the collection's sets.
            c.Sets.Sum(s => s.Items.Count),
            db.UserItems.Count(ui => ui.UserId == userId && ui.CatalogItem.Set.CollectionId == c.Id),
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
        int OwnedCount,
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
            OwnedCount,
            Mine: OwnedCount > 0,
            IsFavorite,
            Popularity: totalUsers == 0 ? 0 : (int)Math.Round(100.0 * CollectorCount / totalUsers));
    }
}

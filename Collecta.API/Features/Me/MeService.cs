using Collecta.API.Domain;
using Collecta.API.Features.Catalog;
using Collecta.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Collecta.API.Features.Me;

public enum WishResult
{
    Done,
    NotFound,
    /// <summary>The user already owns the item; owning and wishing exclude each other.</summary>
    AlreadyOwned,
}

/// <summary>What the signed-in user owns, wants and stars.</summary>
public sealed class MeService(AppDbContext db)
{
    /// <returns>False when the item does not exist.</returns>
    public async Task<bool> SetCopiesAsync(Guid userId, Guid itemId, int copies, CancellationToken ct)
    {
        if (!await db.CatalogItems.AnyAsync(i => i.Id == itemId, ct))
        {
            return false;
        }

        var owned = await db.UserItems.FindAsync([userId, itemId], ct);
        if (copies == 0)
        {
            if (owned is not null)
            {
                db.UserItems.Remove(owned);
            }
        }
        else if (owned is null)
        {
            db.UserItems.Add(new UserItem { UserId = userId, CatalogItemId = itemId, Quantity = copies });
            // Got it: it is no longer something the user is looking for.
            await db.WishlistItems.Where(w => w.UserId == userId && w.CatalogItemId == itemId).ExecuteDeleteAsync(ct);
        }
        else
        {
            owned.Quantity = copies;
        }

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<ItemResponse>> ListRecentAsync(Guid userId, int limit, CancellationToken ct)
    {
        var rows = await db.UserItems
            .Where(ui => ui.UserId == userId)
            .OrderByDescending(ui => ui.AcquiredAt)
            .Take(limit)
            .Select(ui => ui.CatalogItem)
            .ForUser(db, userId)
            .ToListAsync(ct);
        return rows.ConvertAll(row => row.ToResponse());
    }

    /// <returns>Items the user wants and does not own yet, most recently wished first.</returns>
    public async Task<IReadOnlyList<ItemResponse>> ListWishlistAsync(Guid userId, CancellationToken ct)
    {
        var rows = await db.WishlistItems
            .Where(w => w.UserId == userId
                && !db.UserItems.Any(ui => ui.UserId == userId && ui.CatalogItemId == w.CatalogItemId))
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => w.CatalogItem)
            .ForUser(db, userId)
            .ToListAsync(ct);
        return rows.ConvertAll(row => row.ToResponse());
    }

    public async Task<WishResult> AddToWishlistAsync(Guid userId, Guid itemId, CancellationToken ct)
    {
        if (!await db.CatalogItems.AnyAsync(i => i.Id == itemId, ct))
        {
            return WishResult.NotFound;
        }
        if (await db.UserItems.AnyAsync(ui => ui.UserId == userId && ui.CatalogItemId == itemId, ct))
        {
            return WishResult.AlreadyOwned;
        }
        if (await db.WishlistItems.FindAsync([userId, itemId], ct) is null)
        {
            db.WishlistItems.Add(new WishlistItem { UserId = userId, CatalogItemId = itemId });
            await db.SaveChangesAsync(ct);
        }
        return WishResult.Done;
    }

    /// <summary>Idempotent: removing something that is not on the list is not an error.</summary>
    public Task RemoveFromWishlistAsync(Guid userId, Guid itemId, CancellationToken ct) =>
        db.WishlistItems.Where(w => w.UserId == userId && w.CatalogItemId == itemId).ExecuteDeleteAsync(ct);

    /// <returns>False when the collection does not exist.</returns>
    public async Task<bool> SetFavoriteAsync(Guid userId, Guid collectionId, bool favorite, CancellationToken ct)
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
}

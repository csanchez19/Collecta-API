using System.Text.Json;
using Collecta.API.Domain;
using Collecta.API.Infrastructure.Data;

namespace Collecta.API.Features.Catalog;

/// <summary>
/// The one projection every item list uses (set album, recent, wishlist…), so
/// they all return the same <see cref="ItemResponse"/> shape. Filter and sort on
/// the entity first: EF cannot translate operators applied after a projection
/// into a positional record.
/// </summary>
public static class ItemQueries
{
    /// <summary>Album order: "1, 2, … 10" and "001 … 165, TG01" rather than plain string order.</summary>
    public static IQueryable<CatalogItem> InAlbumOrder(this IQueryable<CatalogItem> items) =>
        items.OrderBy(i => i.Code!.Length).ThenBy(i => i.Code).ThenBy(i => i.Name);

    /// <param name="userId">The caller, or <see cref="Guid.Empty"/> for anonymous visitors (matches no rows).</param>
    public static IQueryable<ItemRow> ForUser(this IQueryable<CatalogItem> items, AppDbContext db, Guid userId) =>
        items.Select(i => new ItemRow(
            i.Id,
            i.SetId,
            i.Set.CollectionId,
            i.Set.Name,
            i.Set.Collection.AccentHue,
            i.Name,
            i.Code,
            i.Rarity,
            i.ImageUrl,
            i.ThumbnailUrl,
            i.MarketValueCents,
            i.Attributes,
            db.UserItems
                .Where(ui => ui.UserId == userId && ui.CatalogItemId == i.Id)
                .Select(ui => ui.Quantity)
                .FirstOrDefault(),
            db.WishlistItems.Any(w => w.UserId == userId && w.CatalogItemId == i.Id)));

    public sealed record ItemRow(
        Guid Id,
        Guid SetId,
        Guid CollectionId,
        string SetName,
        int AccentHue,
        string Name,
        string? Code,
        string? Rarity,
        string? ImageUrl,
        string? ThumbnailUrl,
        long? MarketValueCents,
        string? Attributes,
        int OwnedCopies,
        bool InWishlist)
    {
        public ItemResponse ToResponse(decimal? priceChangePct = null) => new(
            Id,
            SetId,
            CollectionId,
            SetName,
            AccentHue,
            Name,
            Code,
            Rarity,
            RarityTiers.For(Rarity),
            ImageUrl,
            ThumbnailUrl,
            MarketValueCents,
            priceChangePct,
            Attributes is null ? null : JsonSerializer.Deserialize<JsonElement>(Attributes),
            OwnedCopies,
            // Owning an item takes it off the wishlist; never report both.
            IsWished: InWishlist && OwnedCopies == 0);
    }
}

using System.Text.Json;

namespace Collecta.API.Features.Catalog;

/// <summary>A set plus the caller's progress in it (zeros for anonymous visitors).</summary>
public sealed record SetResponse(
    Guid Id,
    Guid CollectionId,
    string Name,
    string? Series,
    DateOnly? ReleaseDate,
    string? LogoUrl,
    string? SymbolUrl,
    int TotalCount,
    int OwnedCount,
    int WishedCount,
    long OwnedValueCents);

/// <summary>A catalog item plus what the caller has of it (zeros/false for anonymous visitors).</summary>
/// <param name="RarityTier">0 = unknown, 1 = common … 3 = premium (double rare and above).</param>
/// <param name="PriceChangePct">Change over ~30 days; only on the item detail, null without history.</param>
/// <param name="Attributes">Kind-specific details as imported (hp, types, stage… for cards).</param>
public sealed record ItemResponse(
    Guid Id,
    Guid SetId,
    Guid CollectionId,
    string SetName,
    int AccentHue,
    string Name,
    string? Code,
    string? Rarity,
    int RarityTier,
    string? ImageUrl,
    string? ThumbnailUrl,
    long? MarketValueCents,
    decimal? PriceChangePct,
    JsonElement? Attributes,
    int OwnedCopies,
    bool IsWished);

public sealed record PricePointResponse(DateOnly Date, long ValueCents);

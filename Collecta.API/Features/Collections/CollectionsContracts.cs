using Collecta.API.Domain;

namespace Collecta.API.Features.Collections;

public sealed record CollectionResponse(
    Guid Id,
    string Name,
    CollectionKind Kind,
    string KindLabel,
    int AccentHue,
    int TotalCount,
    int OwnedCount,
    bool Mine,
    bool IsFavorite,
    int Popularity);

public sealed record CatalogItemResponse(
    Guid Id,
    string Name,
    string? Code,
    string? ImageUrl,
    long? MarketValueCents,
    int OwnedQuantity);

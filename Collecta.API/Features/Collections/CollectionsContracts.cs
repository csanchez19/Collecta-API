using Collecta.API.Domain;

namespace Collecta.API.Features.Collections;

/// <summary>A collection plus the caller's progress in it (zeros/false for anonymous visitors).</summary>
/// <param name="Popularity">0–100: share of users who own at least one of its items.</param>
public sealed record CollectionResponse(
    Guid Id,
    string Name,
    CollectionKind Kind,
    string KindLabel,
    int AccentHue,
    int TotalCount,
    int SetCount,
    int OwnedCount,
    long OwnedValueCents,
    bool Mine,
    bool IsFavorite,
    int Popularity);

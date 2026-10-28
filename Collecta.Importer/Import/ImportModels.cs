using Collecta.API.Domain;

namespace Collecta.Importer.Import;

// Source-neutral shape of what gets imported. A source (TCGdex today, others
// tomorrow) only has to produce these; CatalogImporter does the database work.

/// <summary>The collection a source fills. One per source + language: each language is its own catalog.</summary>
public sealed record ImportedCollection(
    string Source,
    string ExternalId,
    string Name,
    CollectionKind Kind,
    string KindLabel,
    int AccentHue);

/// <summary>A set as listed by the source, before loading its items.</summary>
/// <param name="Key">The source's own id, used for <c>--set</c> filters and logs (e.g. "sv03.5").</param>
/// <param name="ItemCount">Items the source says the set has; lets <c>--skip-complete</c> avoid re-downloading.</param>
public sealed record ImportedSetRef(string Key, string ExternalId, string Name, int ItemCount);

public sealed record ImportedSet(
    string ExternalId,
    string Name,
    string? Series,
    DateOnly? ReleaseDate,
    string? LogoUrl,
    string? SymbolUrl);

public sealed record ImportedItem(
    string ExternalId,
    string Name,
    string? Code,
    string? Rarity,
    string? ImageUrl,
    string? ThumbnailUrl,
    long? MarketValueCents,
    string? AttributesJson);

public sealed record ImportedSetContent(ImportedSet Set, IReadOnlyList<ImportedItem> Items);

/// <summary>A catalog to import from: lists its sets and loads one set with all its items.</summary>
public interface ICatalogSource
{
    ImportedCollection Collection { get; }
    Task<IReadOnlyList<ImportedSetRef>> ListSetsAsync(CancellationToken ct);
    Task<ImportedSetContent> LoadSetAsync(ImportedSetRef set, CancellationToken ct);
}

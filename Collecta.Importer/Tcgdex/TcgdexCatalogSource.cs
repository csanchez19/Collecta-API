using System.Globalization;
using System.Text.Json;
using Collecta.API.Domain;
using Collecta.Importer.Import;

namespace Collecta.Importer.Tcgdex;

/// <summary>
/// Pokémon TCG from TCGdex, in one language. Each language is its own
/// collection with its own sets and items: an English and a Spanish Charizard
/// are different pieces. External ids carry the language ("en:sv03.5-006")
/// so languages never collide.
/// </summary>
internal sealed class TcgdexCatalogSource(TcgdexClient client, string language, int concurrency) : ICatalogSource
{
    public const string SourceName = "tcgdex";

    /// <summary>Hue shared by every Pokémon collection (the warm coral of the design).</summary>
    private const int PokemonHue = 30;

    private static readonly Dictionary<string, string> LanguageNames = new()
    {
        ["en"] = "inglés",
        ["es"] = "español",
        ["fr"] = "francés",
        ["de"] = "alemán",
        ["it"] = "italiano",
        ["pt"] = "portugués",
        ["ja"] = "japonés",
    };

    private static readonly JsonSerializerOptions AttributesJson = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public ImportedCollection Collection { get; } = new(
        SourceName,
        $"pokemon:{language}",
        $"Pokémon TCG ({language.ToUpperInvariant()})",
        CollectionKind.Card,
        LanguageNames.TryGetValue(language, out var name) ? $"Cartas en {name}" : $"Cartas ({language})",
        PokemonHue);

    public async Task<IReadOnlyList<ImportedSetRef>> ListSetsAsync(CancellationToken ct)
    {
        var sets = await client.GetSetsAsync(language, ct);
        return sets
            .Select(set => new ImportedSetRef(set.Id, ExternalId(set.Id), set.Name, set.CardCount?.Total ?? 0))
            .ToList();
    }

    public async Task<ImportedSetContent> LoadSetAsync(ImportedSetRef setRef, CancellationToken ct)
    {
        var set = await client.GetSetAsync(language, setRef.Key, ct)
            ?? throw new InvalidOperationException($"TCGdex no encuentra el set {setRef.Key} en '{language}'.");

        var briefs = set.Cards ?? [];
        var items = new ImportedItem[briefs.Count];
        // Prices and details only come with each card: fetch them in parallel, keeping album order.
        await Parallel.ForEachAsync(
            Enumerable.Range(0, briefs.Count),
            new ParallelOptions { MaxDegreeOfParallelism = concurrency, CancellationToken = ct },
            async (index, token) =>
            {
                var brief = briefs[index];
                var card = await client.GetCardAsync(language, brief.Id, token);
                items[index] = card is null ? FromBrief(brief) : FromCard(card);
            });

        var imported = new ImportedSet(
            ExternalId(set.Id),
            set.Name,
            set.Serie?.Name,
            DateOnly.TryParse(set.ReleaseDate, CultureInfo.InvariantCulture, out var date) ? date : null,
            await ExistingAssetAsync(set.Logo, ct),
            await ExistingAssetAsync(set.Symbol, ct));
        return new ImportedSetContent(imported, items);
    }

    /// <summary>The asset URL with its extension, or null if TCGdex's CDN does not serve it.</summary>
    private async Task<string?> ExistingAssetAsync(string? baseUrl, CancellationToken ct)
    {
        var url = Asset(baseUrl);
        return url is not null && await client.AssetExistsAsync(url, ct) ? url : null;
    }

    private string ExternalId(string tcgdexId) => $"{language}:{tcgdexId}";

    private ImportedItem FromBrief(TcgdexCardBrief brief) => new(
        ExternalId(brief.Id),
        brief.Name,
        brief.LocalId,
        Rarity: null,
        Image(brief.Image, "high"),
        Image(brief.Image, "low"),
        MarketValueCents: null,
        AttributesJson: null);

    private ImportedItem FromCard(TcgdexCard card)
    {
        var attributes = new Dictionary<string, object?>
        {
            ["category"] = card.Category,
            ["hp"] = card.Hp,
            ["types"] = card.Types is { Count: > 0 } ? card.Types : null,
            ["stage"] = card.Stage,
            ["evolveFrom"] = card.EvolveFrom,
            ["suffix"] = card.Suffix,
            ["trainerType"] = card.TrainerType,
            ["energyType"] = card.EnergyType,
            ["illustrator"] = card.Illustrator,
            ["regulationMark"] = card.RegulationMark,
            ["dexId"] = card.DexId is { Count: > 0 } ? card.DexId : null,
        }
        .Where(pair => pair.Value is not null)
        .ToDictionary(pair => pair.Key, pair => pair.Value);

        return new ImportedItem(
            ExternalId(card.Id),
            card.Name,
            card.LocalId,
            card.Rarity,
            Image(card.Image, "high"),
            Image(card.Image, "low"),
            PriceCents(card.Pricing?.Cardmarket),
            attributes.Count > 0 ? JsonSerializer.Serialize(attributes, AttributesJson) : null);
    }

    /// <summary>Cardmarket trend price (EUR), falling back to the average.</summary>
    private static long? PriceCents(TcgdexCardmarketPrice? price)
    {
        if (price is null || (price.Unit is not null && price.Unit != "EUR"))
        {
            return null;
        }
        var euros = price.Trend is > 0 ? price.Trend : price.Avg;
        return euros is > 0 ? (long)Math.Round(euros.Value * 100) : null;
    }

    // TCGdex asset URLs come without quality/extension: https://tcgdex.dev/assets
    private static string? Image(string? baseUrl, string quality) =>
        baseUrl is null ? null : $"{baseUrl}/{quality}.webp";

    private static string? Asset(string? baseUrl) => baseUrl is null ? null : $"{baseUrl}.webp";
}

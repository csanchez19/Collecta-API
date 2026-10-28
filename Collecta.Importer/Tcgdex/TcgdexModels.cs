namespace Collecta.Importer.Tcgdex;

// The parts of TCGdex's REST responses the importer uses (https://tcgdex.dev/rest).

internal sealed record TcgdexCardCount(int Total, int Official);

internal sealed record TcgdexSetBrief(string Id, string Name, TcgdexCardCount? CardCount);

internal sealed record TcgdexSerie(string Id, string Name);

internal sealed record TcgdexCardBrief(string Id, string LocalId, string Name, string? Image);

internal sealed record TcgdexSet(
    string Id,
    string Name,
    string? Logo,
    string? Symbol,
    string? ReleaseDate,
    TcgdexSerie? Serie,
    TcgdexCardCount? CardCount,
    List<TcgdexCardBrief>? Cards);

internal sealed record TcgdexCardmarketPrice(decimal? Avg, decimal? Trend, string? Unit);

internal sealed record TcgdexPricing(TcgdexCardmarketPrice? Cardmarket);

internal sealed record TcgdexCard(
    string Id,
    string LocalId,
    string Name,
    string? Image,
    string? Category,
    string? Rarity,
    string? Illustrator,
    int? Hp,
    List<string>? Types,
    string? Stage,
    string? EvolveFrom,
    string? Suffix,
    string? RegulationMark,
    string? TrainerType,
    string? EnergyType,
    List<int>? DexId,
    TcgdexPricing? Pricing);

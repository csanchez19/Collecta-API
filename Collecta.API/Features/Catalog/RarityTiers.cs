namespace Collecta.API.Features.Catalog;

/// <summary>
/// Collapses source rarity names ("Common", "Double rare", "Special illustration rare"…)
/// into three tiers every client can draw the same way.
/// </summary>
public static class RarityTiers
{
    private static readonly string[] Premium =
    [
        "double", "ultra", "illustration", "secret", "hyper", "shiny", "radiant",
        "amazing", "ace", "crown", "full art", "legend", "prime", "lv.x", "star",
    ];

    public static int For(string? rarity)
    {
        if (string.IsNullOrWhiteSpace(rarity))
        {
            return 0;
        }

        var name = rarity.Trim().ToLowerInvariant();
        if (name is "none")
        {
            return 0;
        }
        if (name is "common" or "uncommon")
        {
            return 1;
        }
        return Premium.Any(name.Contains) ? 3 : 2;
    }
}

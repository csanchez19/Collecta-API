namespace Collecta.API.Domain;

/// <summary>
/// Market value of an item on one day, for the price history chart. Importers
/// write at most one per item and day (re-running the same day overwrites it).
/// The current price stays on <see cref="CatalogItem.MarketValueCents"/>.
/// </summary>
public class PriceSnapshot
{
    public Guid CatalogItemId { get; set; }
    public DateOnly Date { get; set; }
    // Whole euro cents, like CatalogItem.MarketValueCents.
    public long ValueCents { get; set; }

    public CatalogItem CatalogItem { get; set; } = null!;
}

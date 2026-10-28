namespace Collecta.API.Domain;

public class CatalogItem
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid SetId { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Number inside the set, e.g. "025".</summary>
    public string? Code { get; set; }
    public string? Rarity { get; set; }
    public string? ImageUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    // Stored in whole cents so money stays exact and sums never drift.
    public long? MarketValueCents { get; set; }
    /// <summary>Kind-specific details as JSON (HP, types, attacks for cards; scale, series for cars…).</summary>
    public string? Attributes { get; set; }
    public string? Source { get; set; }
    public string? ExternalId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public CollectionSet Set { get; set; } = null!;
    public ICollection<PriceSnapshot> PriceHistory { get; set; } = [];
}

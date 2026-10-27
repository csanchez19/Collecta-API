namespace Collecta.API.Domain;

public class CatalogItem
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid CollectionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? ImageUrl { get; set; }
    // Stored in cents: SQLite cannot sort or aggregate decimal server-side.
    public long? MarketValueCents { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Collection Collection { get; set; } = null!;
}

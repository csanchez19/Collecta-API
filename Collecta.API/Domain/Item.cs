namespace Collecta.API.Domain;

public class Item
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CollectionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal? MarketValue { get; set; }
    public bool Owned { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Collection? Collection { get; set; }
}

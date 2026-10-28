namespace Collecta.API.Domain;

/// <summary>Top-level catalog theme (Pokémon TCG, Hot Wheels…), grouping its sets. Not owned by any user.</summary>
public class Collection
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = string.Empty;
    public CollectionKind Kind { get; set; }
    public string KindLabel { get; set; } = string.Empty;
    public int AccentHue { get; set; }
    public string? Source { get; set; }
    public string? ExternalId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CollectionSet> Sets { get; set; } = [];
}

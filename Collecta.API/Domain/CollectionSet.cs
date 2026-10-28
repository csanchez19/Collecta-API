namespace Collecta.API.Domain;

/// <summary>A release inside a collection (e.g. the "151" expansion of Pokémon TCG).</summary>
public class CollectionSet
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid CollectionId { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Optional grouping inside the collection, e.g. "Escarlata y Púrpura".</summary>
    public string? Series { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public string? LogoUrl { get; set; }
    public string? SymbolUrl { get; set; }
    public string? Source { get; set; }
    public string? ExternalId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Collection Collection { get; set; } = null!;
    public ICollection<CatalogItem> Items { get; set; } = [];
}

namespace Collecta.API.Domain;

/// <summary>Catalog theme published by an administrator (Hot Wheels, Pokémon 151…). Not owned by any user.</summary>
public class Collection
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = string.Empty;
    public CollectionKind Kind { get; set; }
    public string KindLabel { get; set; } = string.Empty;
    public int AccentHue { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CatalogItem> Items { get; set; } = [];
}

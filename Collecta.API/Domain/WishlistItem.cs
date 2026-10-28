namespace Collecta.API.Domain;

/// <summary>"User is looking for this item." Per user, like <see cref="UserItem"/>.</summary>
public class WishlistItem
{
    public Guid UserId { get; set; }
    public Guid CatalogItemId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public CatalogItem CatalogItem { get; set; } = null!;
}

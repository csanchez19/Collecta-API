namespace Collecta.API.Domain;

public class UserItem
{
    public Guid UserId { get; set; }
    public Guid CatalogItemId { get; set; }
    public int Quantity { get; set; } = 1;
    public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public CatalogItem CatalogItem { get; set; } = null!;
}

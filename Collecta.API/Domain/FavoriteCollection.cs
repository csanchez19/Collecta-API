namespace Collecta.API.Domain;

public class FavoriteCollection
{
    public string UserId { get; set; } = string.Empty;
    public string CollectionId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
    public Collection? Collection { get; set; }
}

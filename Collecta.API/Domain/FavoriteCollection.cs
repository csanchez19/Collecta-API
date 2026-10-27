namespace Collecta.API.Domain;

public class FavoriteCollection
{
    public Guid UserId { get; set; }
    public Guid CollectionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Collection Collection { get; set; } = null!;
}

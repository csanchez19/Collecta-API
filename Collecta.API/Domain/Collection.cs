namespace Collecta.API.Domain;

public class Collection
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty; // car, card, spinner, figure, brick
    public string KindLabel { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int OwnedCount { get; set; }
    public int AccentHue { get; set; } // 0-360 for color gradient
    public int Popularity { get; set; }
    public bool Mine { get; set; } // true if user created it
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
    public ICollection<Item> Items { get; set; } = [];
}

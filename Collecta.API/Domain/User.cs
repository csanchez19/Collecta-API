namespace Collecta.API.Domain;

public class User
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserItem> OwnedItems { get; set; } = [];
    public ICollection<FavoriteCollection> Favorites { get; set; } = [];
    public ICollection<WishlistItem> Wishlist { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}

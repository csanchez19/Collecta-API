using Collecta.API.Domain;
using Microsoft.EntityFrameworkCore;

namespace Collecta.API.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<CatalogItem> CatalogItems => Set<CatalogItem>();
    public DbSet<UserItem> UserItems => Set<UserItem>();
    public DbSet<FavoriteCollection> FavoriteCollections => Set<FavoriteCollection>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(user =>
        {
            user.Property(u => u.Email).HasMaxLength(256);
            user.Property(u => u.DisplayName).HasMaxLength(50);
            user.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Collection>(collection =>
        {
            collection.Property(c => c.Name).HasMaxLength(100);
            collection.Property(c => c.KindLabel).HasMaxLength(100);
            collection.Property(c => c.Kind).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<CatalogItem>(item =>
        {
            item.Property(i => i.Name).HasMaxLength(150);
            item.Property(i => i.Code).HasMaxLength(32);
            item.Property(i => i.ImageUrl).HasMaxLength(2048);
            item.HasOne(i => i.Collection)
                .WithMany(c => c.Items)
                .HasForeignKey(i => i.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserItem>(owned =>
        {
            owned.HasKey(ui => new { ui.UserId, ui.CatalogItemId });
            owned.HasOne(ui => ui.User)
                .WithMany(u => u.OwnedItems)
                .HasForeignKey(ui => ui.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            owned.HasOne(ui => ui.CatalogItem)
                .WithMany()
                .HasForeignKey(ui => ui.CatalogItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FavoriteCollection>(favorite =>
        {
            favorite.HasKey(f => new { f.UserId, f.CollectionId });
            favorite.HasOne(f => f.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            favorite.HasOne(f => f.Collection)
                .WithMany()
                .HasForeignKey(f => f.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(token =>
        {
            token.Property(t => t.TokenHash).HasMaxLength(64);
            token.HasIndex(t => t.TokenHash).IsUnique();
            token.HasOne(t => t.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

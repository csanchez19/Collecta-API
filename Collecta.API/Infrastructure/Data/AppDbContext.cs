using Collecta.API.Domain;
using Microsoft.EntityFrameworkCore;

namespace Collecta.API.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<CollectionSet> CollectionSets => Set<CollectionSet>();
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

        // Imported rows are identified by (Source, ExternalId) so re-imports update instead of duplicating.
        // Rows added by hand leave both null; SQLite treats NULLs as distinct in a unique index.
        modelBuilder.Entity<Collection>(collection =>
        {
            collection.Property(c => c.Name).HasMaxLength(100);
            collection.Property(c => c.KindLabel).HasMaxLength(100);
            collection.Property(c => c.Kind).HasConversion<string>().HasMaxLength(20);
            collection.Property(c => c.Source).HasMaxLength(20);
            collection.Property(c => c.ExternalId).HasMaxLength(64);
            collection.HasIndex(c => new { c.Source, c.ExternalId }).IsUnique();
        });

        modelBuilder.Entity<CollectionSet>(set =>
        {
            set.Property(s => s.Name).HasMaxLength(100);
            set.Property(s => s.Series).HasMaxLength(100);
            set.Property(s => s.LogoUrl).HasMaxLength(2048);
            set.Property(s => s.SymbolUrl).HasMaxLength(2048);
            set.Property(s => s.Source).HasMaxLength(20);
            set.Property(s => s.ExternalId).HasMaxLength(64);
            set.HasIndex(s => new { s.Source, s.ExternalId }).IsUnique();
            set.HasOne(s => s.Collection)
                .WithMany(c => c.Sets)
                .HasForeignKey(s => s.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CatalogItem>(item =>
        {
            item.Property(i => i.Name).HasMaxLength(150);
            item.Property(i => i.Code).HasMaxLength(32);
            item.Property(i => i.Rarity).HasMaxLength(50);
            item.Property(i => i.ImageUrl).HasMaxLength(2048);
            item.Property(i => i.ThumbnailUrl).HasMaxLength(2048);
            item.Property(i => i.Source).HasMaxLength(20);
            item.Property(i => i.ExternalId).HasMaxLength(64);
            item.HasIndex(i => new { i.Source, i.ExternalId }).IsUnique();
            item.HasOne(i => i.Set)
                .WithMany(s => s.Items)
                .HasForeignKey(i => i.SetId)
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

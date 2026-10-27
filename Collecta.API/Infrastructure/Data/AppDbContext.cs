using Collecta.API.Domain;
using Microsoft.EntityFrameworkCore;

namespace Collecta.API.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Collection> Collections { get; set; } = null!;
    public DbSet<Item> Items { get; set; } = null!;
    public DbSet<FavoriteCollection> FavoriteCollections { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // FavoriteCollection composite key
        modelBuilder.Entity<FavoriteCollection>()
            .HasKey(fc => new { fc.UserId, fc.CollectionId });

        // User → Collections (one-to-many)
        modelBuilder.Entity<Collection>()
            .HasOne(c => c.User)
            .WithMany(u => u.Collections)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Collection → Items (one-to-many)
        modelBuilder.Entity<Item>()
            .HasOne(i => i.Collection)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.CollectionId)
            .OnDelete(DeleteBehavior.Cascade);

        // User → FavoriteCollection
        modelBuilder.Entity<FavoriteCollection>()
            .HasOne(fc => fc.User)
            .WithMany(u => u.Favorites)
            .HasForeignKey(fc => fc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Collection → FavoriteCollection
        modelBuilder.Entity<FavoriteCollection>()
            .HasOne(fc => fc.Collection)
            .WithMany()
            .HasForeignKey(fc => fc.CollectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

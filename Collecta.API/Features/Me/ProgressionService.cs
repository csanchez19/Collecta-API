using Collecta.API.Features.Catalog;
using Collecta.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Collecta.API.Features.Me;

/// <summary>
/// Level, XP and achievements, derived from what the user owns — nothing is
/// stored, so they can never drift from the collection. The frontend shows the
/// same XP reward (keep <see cref="XpPerItem"/> in sync with its XP_PER_NEW_ITEM).
/// </summary>
public sealed class ProgressionService(AppDbContext db)
{
    public const int XpPerItem = 40;
    public const int XpLevelSize = 250;

    /// <summary>Pockets on one album page (3 × 3), as the app shows a set.</summary>
    private const int AlbumPageSize = 9;

    public async Task<ProgressionResponse> GetAsync(Guid userId, CancellationToken ct)
    {
        var owned = await db.UserItems
            .Where(ui => ui.UserId == userId)
            .Select(ui => new { ui.CatalogItemId, ui.Quantity, ui.CatalogItem.SetId, ui.CatalogItem.Set.CollectionId })
            .ToListAsync(ct);

        var ownedIds = owned.Select(o => o.CatalogItemId).ToHashSet();
        var setIds = owned.Select(o => o.SetId).Distinct().ToList();

        // Only sets the user has started can hold a complete page or set.
        var albums = (await db.CatalogItems
                .Where(i => setIds.Contains(i.SetId))
                .InAlbumOrder()
                .Select(i => new { i.Id, i.SetId })
                .ToListAsync(ct))
            .GroupBy(i => i.SetId, i => ownedIds.Contains(i.Id))
            .Select(set => set.ToList())
            .ToList();

        var bestSet = albums.Select(Ratio).DefaultIfEmpty(0).Max();
        var bestPage = albums
            .SelectMany(set => set.Chunk(AlbumPageSize).Where(page => page.Length == AlbumPageSize))
            .Select(page => Ratio(page))
            .DefaultIfEmpty(0)
            .Max();
        var collections = owned.Select(o => o.CollectionId).Distinct().Count();

        AchievementResponse[] achievements =
        [
            Achievement("first", "Primera pieza", "1", owned.Count >= 1 ? 1 : 0),
            Achievement("duplicate", "Primer repetido", "×2", owned.Any(o => o.Quantity > 1) ? 1 : 0),
            Achievement("three-collections", "Tres colecciones", "3", collections / 3.0),
            Achievement("page", "Página completa", "9/9", bestPage),
            Achievement("fifty", "50 piezas", "50", owned.Count / 50.0),
            Achievement("set", "Set completo", "1/1", bestSet),
        ];

        var xp = owned.Count * XpPerItem;
        return new ProgressionResponse(xp / XpLevelSize + 1, xp % XpLevelSize, XpLevelSize, achievements);
    }

    private static double Ratio(IReadOnlyCollection<bool> pockets) =>
        pockets.Count == 0 ? 0 : (double)pockets.Count(owned => owned) / pockets.Count;

    private static AchievementResponse Achievement(string id, string name, string mark, double progress)
    {
        var clamped = Math.Min(1, progress);
        return new AchievementResponse(id, name, mark, clamped >= 1, Math.Round(clamped, 3));
    }
}

using System.Text.Json.Nodes;
using Collecta.API.Domain;
using Collecta.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Collecta.Importer.Import;

/// <summary>What to import from a source and how.</summary>
/// <param name="SetKeys">Only these sets (source ids); empty = all.</param>
/// <param name="PricesOnly">Refresh market values of items already imported; touch nothing else.</param>
/// <param name="SkipComplete">Skip sets that already have every item, to resume a cut-short run cheaply.</param>
public sealed record ImportRun(IReadOnlySet<string> SetKeys, bool PricesOnly, bool SkipComplete);

public sealed record ImportSummary(int Sets, int Added, int Updated, int Skipped);

/// <summary>
/// Writes any <see cref="ICatalogSource"/> into the database. Rows are matched
/// by (Source, ExternalId), so re-running updates instead of duplicating.
/// Nothing is ever deleted: users' collections point at these rows.
/// Each set is saved in its own transaction, so a cut-short run keeps every
/// finished set and can simply be run again.
/// </summary>
public sealed class CatalogImporter(Func<AppDbContext> createDb, TextWriter log)
{
    // Column limits from AppDbContext; longer source values are clipped instead of failing the set.
    private const int SetNameMax = 100;
    private const int SeriesMax = 100;
    private const int UrlMax = 2048;
    private const int ItemNameMax = 150;
    private const int CodeMax = 32;
    private const int RarityMax = 50;

    public async Task<ImportSummary> RunAsync(ICatalogSource source, ImportRun run, CancellationToken ct)
    {
        var collectionId = await UpsertCollectionAsync(source.Collection, run.PricesOnly, ct);
        if (collectionId is null)
        {
            log.WriteLine($"La colección {source.Collection.Name} todavía no existe: importa el catálogo antes de actualizar precios.");
            return new ImportSummary(0, 0, 0, 0);
        }

        var sets = (await source.ListSetsAsync(ct))
            .Where(set => run.SetKeys.Count == 0 || run.SetKeys.Contains(set.Key))
            .ToList();
        log.WriteLine($"{source.Collection.Name}: {sets.Count} sets.");

        int added = 0, updated = 0, skipped = 0;
        for (var i = 0; i < sets.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var setRef = sets[i];
            var prefix = $"[{i + 1}/{sets.Count}] {setRef.Key} · {setRef.Name}";

            if (run.SkipComplete && await IsCompleteAsync(source.Collection.Source, setRef, ct))
            {
                skipped++;
                log.WriteLine($"{prefix}: completo, se salta.");
                continue;
            }

            var content = await source.LoadSetAsync(setRef, ct);
            var (setAdded, setUpdated) = await SaveSetAsync(source.Collection.Source, collectionId.Value, content, run.PricesOnly, ct);
            added += setAdded;
            updated += setUpdated;
            log.WriteLine($"{prefix}: {content.Items.Count} piezas ({setAdded} nuevas, {setUpdated} actualizadas).");
        }

        return new ImportSummary(sets.Count, added, updated, skipped);
    }

    private async Task<Guid?> UpsertCollectionAsync(ImportedCollection imported, bool pricesOnly, CancellationToken ct)
    {
        await using var db = createDb();
        var collection = await db.Collections.SingleOrDefaultAsync(
            c => c.Source == imported.Source && c.ExternalId == imported.ExternalId, ct);

        if (pricesOnly)
        {
            return collection?.Id;
        }

        if (collection is null)
        {
            // Name, label and hue are only set on creation: an admin may have edited them since.
            collection = new Collection
            {
                Source = imported.Source,
                ExternalId = imported.ExternalId,
                Name = imported.Name,
                Kind = imported.Kind,
                KindLabel = imported.KindLabel,
                AccentHue = imported.AccentHue,
            };
            db.Collections.Add(collection);
            await db.SaveChangesAsync(ct);
            log.WriteLine($"Colección creada: {collection.Name}.");
        }

        return collection.Id;
    }

    private async Task<bool> IsCompleteAsync(string source, ImportedSetRef setRef, CancellationToken ct)
    {
        await using var db = createDb();
        var count = await db.CatalogItems.CountAsync(
            item => item.Set.Source == source && item.Set.ExternalId == setRef.ExternalId, ct);
        return setRef.ItemCount > 0 && count >= setRef.ItemCount;
    }

    private async Task<(int Added, int Updated)> SaveSetAsync(
        string source, Guid collectionId, ImportedSetContent content, bool pricesOnly, CancellationToken ct)
    {
        await using var db = createDb();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var set = await db.CollectionSets.SingleOrDefaultAsync(
            s => s.Source == source && s.ExternalId == content.Set.ExternalId, ct);
        if (set is null)
        {
            if (pricesOnly)
            {
                return (0, 0);
            }
            set = new CollectionSet { CollectionId = collectionId, Source = source, ExternalId = content.Set.ExternalId };
            db.CollectionSets.Add(set);
        }

        if (!pricesOnly)
        {
            set.Name = Clip(content.Set.Name, SetNameMax)!;
            set.Series = Clip(content.Set.Series, SeriesMax);
            set.ReleaseDate = content.Set.ReleaseDate;
            set.LogoUrl = Clip(content.Set.LogoUrl, UrlMax);
            set.SymbolUrl = Clip(content.Set.SymbolUrl, UrlMax);
        }

        var existing = await db.CatalogItems
            .Where(item => item.SetId == set.Id && item.Source == source)
            .ToDictionaryAsync(item => item.ExternalId!, ct);

        var added = 0;
        var seen = new HashSet<string>();
        foreach (var imported in content.Items)
        {
            if (!seen.Add(imported.ExternalId))
            {
                continue; // The source listed the same item twice.
            }

            if (!existing.TryGetValue(imported.ExternalId, out var item))
            {
                if (pricesOnly)
                {
                    continue;
                }
                item = new CatalogItem { Source = source, ExternalId = imported.ExternalId };
                set.Items.Add(item);
                added++;
            }

            item.MarketValueCents = imported.MarketValueCents;
            if (!pricesOnly)
            {
                Apply(item, imported);
            }
        }

        var updated = db.ChangeTracker.Entries<CatalogItem>().Count(entry => entry.State == EntityState.Modified);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return (added, updated);
    }

    private static void Apply(CatalogItem item, ImportedItem imported)
    {
        item.Name = Clip(imported.Name, ItemNameMax)!;
        item.Code = Clip(imported.Code, CodeMax);
        item.Rarity = Clip(imported.Rarity, RarityMax);
        item.ImageUrl = Clip(imported.ImageUrl, UrlMax);
        item.ThumbnailUrl = Clip(imported.ThumbnailUrl, UrlMax);
        // PostgreSQL normalises jsonb (key order, spacing): compare by content so a
        // re-run does not rewrite every row.
        if (!SameJson(item.Attributes, imported.AttributesJson))
        {
            item.Attributes = imported.AttributesJson;
        }
    }

    private static bool SameJson(string? stored, string? incoming)
    {
        if (stored is null || incoming is null)
        {
            return stored == incoming;
        }
        return JsonNode.DeepEquals(JsonNode.Parse(stored), JsonNode.Parse(incoming));
    }

    private static string? Clip(string? value, int max) =>
        value is { Length: > 0 } && value.Length > max ? value[..max] : value;
}

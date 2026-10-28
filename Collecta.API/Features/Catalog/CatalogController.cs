using Collecta.API.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Collecta.API.Features.Catalog;

/// <summary>
/// Public catalog: sets, items and prices. Anyone may browse; a signed-in
/// caller also gets their own counts (owned copies, wishlist).
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api")]
public sealed class CatalogController(CatalogService catalog) : ControllerBase
{
    private static readonly Dictionary<string, int> PriceRanges = new()
    {
        ["7d"] = 7,
        ["30d"] = 30,
        ["1y"] = 365,
    };

    [HttpGet("collections/{collectionId:guid}/sets")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SetResponse>>> ListSets(Guid collectionId, CancellationToken ct)
    {
        var sets = await catalog.ListSetsAsync(collectionId, User.FindUserId(), ct);
        return sets is null ? NotFound() : Ok(sets);
    }

    [HttpGet("sets/{setId:guid}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SetResponse>> GetSet(Guid setId, CancellationToken ct)
    {
        var set = await catalog.GetSetAsync(setId, User.FindUserId(), ct);
        return set is null ? NotFound() : Ok(set);
    }

    [HttpGet("sets/{setId:guid}/items")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ItemResponse>>> ListItems(Guid setId, CancellationToken ct)
    {
        var items = await catalog.ListItemsAsync(setId, User.FindUserId(), ct);
        return items is null ? NotFound() : Ok(items);
    }

    [HttpGet("items/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemResponse>> GetItem(Guid itemId, CancellationToken ct)
    {
        var item = await catalog.GetItemAsync(itemId, User.FindUserId(), ct);
        return item is null ? NotFound() : Ok(item);
    }

    /// <param name="range">7d, 30d (default) or 1y.</param>
    [HttpGet("items/{itemId:guid}/prices")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PricePointResponse>>> GetPrices(
        Guid itemId, [FromQuery] string range = "30d", CancellationToken ct = default)
    {
        if (!PriceRanges.TryGetValue(range, out var days))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Periodo no válido. Usa 7d, 30d o 1y.");
        }

        var prices = await catalog.GetPriceHistoryAsync(itemId, days, ct);
        return prices is null ? NotFound() : Ok(prices);
    }
}

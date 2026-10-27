using Collecta.API.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Collecta.API.Features.Collections;

[ApiController]
[Authorize]
[Route("api/collections")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class CollectionsController(CollectionsService collections) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CollectionResponse>>> List(CancellationToken ct) =>
        Ok(await collections.ListAsync(User.GetUserId(), ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CollectionResponse>> Get(Guid id, CancellationToken ct)
    {
        var collection = await collections.GetAsync(id, User.GetUserId(), ct);
        return collection is null ? NotFound() : Ok(collection);
    }

    [HttpGet("{id:guid}/items")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CatalogItemResponse>>> ListItems(Guid id, CancellationToken ct)
    {
        var items = await collections.ListItemsAsync(id, User.GetUserId(), ct);
        return items is null ? NotFound() : Ok(items);
    }

    [HttpPut("{id:guid}/favorite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> AddFavorite(Guid id, CancellationToken ct) => SetFavorite(id, true, ct);

    [HttpDelete("{id:guid}/favorite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> RemoveFavorite(Guid id, CancellationToken ct) => SetFavorite(id, false, ct);

    private async Task<IActionResult> SetFavorite(Guid id, bool favorite, CancellationToken ct) =>
        await collections.SetFavoriteAsync(id, User.GetUserId(), favorite, ct) ? NoContent() : NotFound();
}

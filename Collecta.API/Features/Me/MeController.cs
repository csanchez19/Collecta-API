using Collecta.API.Features.Auth;
using Collecta.API.Features.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Collecta.API.Features.Me;

/// <summary>The signed-in user's own data: owned copies, wishlist, favourites, activity and level.</summary>
[ApiController]
[Authorize]
[Route("api/me")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class MeController(MeService me, ProgressionService progression) : ControllerBase
{
    private const int MaxRecent = 50;

    /// <summary>Sets how many copies the user owns; 0 removes the item. Owning it also takes it off the wishlist.</summary>
    [HttpPut("items/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetCopies(Guid itemId, SetCopiesRequest request, CancellationToken ct) =>
        await me.SetCopiesAsync(User.GetUserId(), itemId, request.Copies, ct) ? NoContent() : NotFound();

    /// <summary>Most recently added items first.</summary>
    [HttpGet("items/recent")]
    public async Task<ActionResult<IReadOnlyList<ItemResponse>>> ListRecent([FromQuery] int limit = 8, CancellationToken ct = default) =>
        Ok(await me.ListRecentAsync(User.GetUserId(), Math.Clamp(limit, 1, MaxRecent), ct));

    [HttpGet("wishlist")]
    public async Task<ActionResult<IReadOnlyList<ItemResponse>>> ListWishlist(CancellationToken ct) =>
        Ok(await me.ListWishlistAsync(User.GetUserId(), ct));

    [HttpPut("wishlist/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddToWishlist(Guid itemId, CancellationToken ct) =>
        await me.AddToWishlistAsync(User.GetUserId(), itemId, ct) switch
        {
            WishResult.Done => NoContent(),
            WishResult.AlreadyOwned => Problem(statusCode: StatusCodes.Status409Conflict, title: "Ya tienes esta pieza en tu colección."),
            _ => NotFound(),
        };

    [HttpDelete("wishlist/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveFromWishlist(Guid itemId, CancellationToken ct)
    {
        await me.RemoveFromWishlistAsync(User.GetUserId(), itemId, ct);
        return NoContent();
    }

    [HttpPut("favorites/{collectionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddFavorite(Guid collectionId, CancellationToken ct) =>
        await me.SetFavoriteAsync(User.GetUserId(), collectionId, true, ct) ? NoContent() : NotFound();

    [HttpDelete("favorites/{collectionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFavorite(Guid collectionId, CancellationToken ct) =>
        await me.SetFavoriteAsync(User.GetUserId(), collectionId, false, ct) ? NoContent() : NotFound();

    [HttpGet("progression")]
    public async Task<ActionResult<ProgressionResponse>> GetProgression(CancellationToken ct) =>
        Ok(await progression.GetAsync(User.GetUserId(), ct));
}

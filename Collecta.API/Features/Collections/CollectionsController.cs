using Collecta.API.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Collecta.API.Features.Collections;

/// <summary>
/// Public list of collections. A signed-in caller also gets their progress and
/// favourites; favourites are changed under <c>/api/me/favorites</c>.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/collections")]
public sealed class CollectionsController(CollectionsService collections) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CollectionResponse>>> List(CancellationToken ct) =>
        Ok(await collections.ListAsync(User.FindUserId(), ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CollectionResponse>> Get(Guid id, CancellationToken ct)
    {
        var collection = await collections.GetAsync(id, User.FindUserId(), ct);
        return collection is null ? NotFound() : Ok(collection);
    }
}

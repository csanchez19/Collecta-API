using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Collecta.API.Features.Auth;

/// <summary>
/// A request that sends a token must send a valid one, even on public endpoints.
/// Without this, an expired token on the public catalog would silently be served
/// as an anonymous visitor (no owned counts) instead of the 401 that makes the
/// app refresh its session.
/// </summary>
public sealed class RejectInvalidTokenFilter : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;
        if (http.Request.Headers.Authorization.Count > 0 && http.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
        }
    }
}

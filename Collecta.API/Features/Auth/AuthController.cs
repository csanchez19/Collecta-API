using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Collecta.API.Features.Auth;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var response = await auth.RegisterAsync(request, ct);
        return response is null
            ? Problem(statusCode: StatusCodes.Status409Conflict, title: "Ya existe una cuenta con ese email.")
            : Ok(response);
    }

    [HttpPost("login")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var response = await auth.LoginAsync(request, ct);
        return response is null
            ? Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Email o contraseña incorrectos.")
            : Ok(response);
    }

    [HttpPost("refresh")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request, CancellationToken ct)
    {
        var response = await auth.RefreshAsync(request.RefreshToken, ct);
        return response is null
            ? Problem(statusCode: StatusCodes.Status401Unauthorized, title: "La sesión ha caducado. Vuelve a iniciar sesión.")
            : Ok(response);
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken ct)
    {
        await auth.LogoutAsync(request.RefreshToken, ct);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken ct)
    {
        var user = await auth.GetUserAsync(User.GetUserId(), ct);
        return user is null ? NotFound() : Ok(user);
    }
}

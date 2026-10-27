using Collecta.API.Domain;
using Collecta.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Collecta.API.Features.Auth;

public sealed class AuthService(AppDbContext db, TokenService tokens)
{
    // Verified when the email does not exist, so a failed login takes the same time either way
    // and response timing cannot reveal which emails are registered.
    private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword("collecta-timing-guard");

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
        {
            return null;
        }

        var user = new User
        {
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        };
        db.Users.Add(user);
        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email, ct);
        var passwordMatches = BCrypt.Net.BCrypt.Verify(request.Password, user?.PasswordHash ?? DummyHash);
        return user is not null && passwordMatches ? await IssueTokensAsync(user, ct) : null;
    }

    public async Task<AuthResponse?> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var hash = TokenService.HashRefreshToken(refreshToken);
        var stored = await db.RefreshTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null || stored.ExpiresAt <= DateTime.UtcNow)
        {
            return null;
        }

        if (stored.RevokedAt is not null)
        {
            // A rotated token is being replayed, so it has leaked: end every session of that user.
            await RevokeActiveTokensAsync(stored.UserId, ct);
            return null;
        }

        stored.RevokedAt = DateTime.UtcNow;
        return await IssueTokensAsync(stored.User, ct);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct)
    {
        var hash = TokenService.HashRefreshToken(refreshToken);
        var now = DateTime.UtcNow;
        await db.RefreshTokens
            .Where(t => t.TokenHash == hash && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
    }

    public Task<UserResponse?> GetUserAsync(Guid userId, CancellationToken ct) =>
        db.Users
            .Where(u => u.Id == userId)
            .Select(u => new UserResponse(u.Id, u.Email, u.DisplayName))
            .SingleOrDefaultAsync(ct);

    private async Task RevokeActiveTokensAsync(Guid userId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken ct)
    {
        var (accessToken, accessExpiresAt) = tokens.CreateAccessToken(user);
        var refreshToken = TokenService.CreateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = TokenService.HashRefreshToken(refreshToken),
            ExpiresAt = tokens.RefreshTokenExpiresAt(),
        });
        await db.SaveChangesAsync(ct);

        return new AuthResponse(
            accessToken,
            accessExpiresAt,
            refreshToken,
            new UserResponse(user.Id, user.Email, user.DisplayName));
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}

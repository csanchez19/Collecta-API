using System.ComponentModel.DataAnnotations;

namespace Collecta.API.Features.Auth;

public sealed record RegisterRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(2), MaxLength(50)] string DisplayName,
    // BCrypt ignores everything past 72 bytes; 64 chars keeps accented passwords under that.
    [Required, MinLength(8), MaxLength(64)] string Password);

public sealed record LoginRequest(
    [Required] string Email,
    [Required] string Password);

public sealed record RefreshRequest([Required] string RefreshToken);

public sealed record UserResponse(Guid Id, string Email, string DisplayName);

public sealed record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    UserResponse User);

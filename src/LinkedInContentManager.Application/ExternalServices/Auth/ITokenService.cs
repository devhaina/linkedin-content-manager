using LinkedInContentManager.Domain.Entities;

namespace LinkedInContentManager.Application.ExternalServices.Auth;

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAtUtc);

public sealed record RefreshToken(string Value, string Hash, DateTimeOffset ExpiresAtUtc);

public interface ITokenService
{
    AccessToken GenerateAccessToken(User user);

    /// <summary>Generates a new opaque refresh token and its hash — only the hash is ever persisted.</summary>
    RefreshToken GenerateRefreshToken();

    string HashRefreshToken(string refreshToken);
}

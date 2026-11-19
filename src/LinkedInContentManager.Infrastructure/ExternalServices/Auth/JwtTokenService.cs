using System.Security.Claims;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using LinkedInContentManager.Application.ExternalServices.Auth;
using LinkedInContentManager.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LinkedInContentManager.Infrastructure.ExternalServices.Auth;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    public AccessToken GenerateAccessToken(User user)
    {
        var o = options.Value;
        var expiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(o.AccessTokenMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(o.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: o.Issuer,
            audience: o.Audience,
            claims: claims,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: credentials);

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }

    public RefreshToken GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var value = Convert.ToBase64String(bytes);
        var expiresAtUtc = DateTimeOffset.UtcNow.AddDays(options.Value.RefreshTokenDays);

        return new RefreshToken(value, HashRefreshToken(value), expiresAtUtc);
    }

    public string HashRefreshToken(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes);
    }
}

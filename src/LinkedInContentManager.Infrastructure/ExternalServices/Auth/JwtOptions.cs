using System.ComponentModel.DataAnnotations;

namespace LinkedInContentManager.Infrastructure.ExternalServices.Auth;

/// <summary>
/// Bound from the <c>Jwt</c> configuration section. <see cref="SigningKey"/> must never be
/// committed with a real value — see appsettings.json's comment and the README's secret
/// management section. Use an environment variable or .NET user-secrets locally, a real
/// secret manager (Key Vault, etc.) in any deployed environment.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required, MinLength(32)]
    public required string SigningKey { get; set; }

    [Required]
    public required string Issuer { get; set; }

    [Required]
    public required string Audience { get; set; }

    public int AccessTokenMinutes { get; set; } = 30;

    public int RefreshTokenDays { get; set; } = 14;
}

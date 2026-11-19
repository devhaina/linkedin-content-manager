using LinkedInContentManager.Domain.Common;
using LinkedInContentManager.Domain.Enums;

namespace LinkedInContentManager.Domain.Entities;

/// <summary>An authenticated user of the dashboard — campaign owner, approver, or admin.</summary>
public class User : BaseEntity
{
    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    public bool IsActive { get; private set; } = true;

    public string? RefreshTokenHash { get; private set; }

    public DateTimeOffset? RefreshTokenExpiresAtUtc { get; private set; }

    private User() { }

    public User(string email, string passwordHash, string displayName, UserRole role)
    {
        Email = string.IsNullOrWhiteSpace(email) ? throw new ArgumentException("Email is required.", nameof(email)) : email;
        PasswordHash = string.IsNullOrWhiteSpace(passwordHash) ? throw new ArgumentException("PasswordHash is required.", nameof(passwordHash)) : passwordHash;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? throw new ArgumentException("DisplayName is required.", nameof(displayName)) : displayName;
        Role = role;
    }

    /// <summary>Used only by deterministic seed data, where a fixed Id/Id and audit timestamp are required.</summary>
    public User(Guid id, string email, string passwordHash, string displayName, UserRole role) : this(email, passwordHash, displayName, role)
    {
        Id = id;
        SetSeedTimestamps(SeedTimestamp);
    }

    /// <summary>Fixed instant every user seed row is stamped with, so the EF Core model is deterministic across builds.</summary>
    public static readonly DateTimeOffset SeedTimestamp = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public void SetRefreshToken(string refreshTokenHash, DateTimeOffset expiresAtUtc)
    {
        RefreshTokenHash = refreshTokenHash;
        RefreshTokenExpiresAtUtc = expiresAtUtc;
        Touch();
    }

    public void RevokeRefreshToken()
    {
        RefreshTokenHash = null;
        RefreshTokenExpiresAtUtc = null;
        Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = string.IsNullOrWhiteSpace(newPasswordHash)
            ? throw new ArgumentException("PasswordHash is required.", nameof(newPasswordHash))
            : newPasswordHash;
        Touch();
    }
}

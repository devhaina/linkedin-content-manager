using LinkedInContentManager.Application.ExternalServices.Auth;
using LinkedInContentManager.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace LinkedInContentManager.Infrastructure.ExternalServices.Auth;

/// <summary>
/// Wraps ASP.NET Core Identity's <see cref="PasswordHasher{TUser}"/> (PBKDF2, salted,
/// versioned) — a real, audited implementation, not a hand-rolled hash.
/// </summary>
public sealed class PasswordHasherAdapter : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string HashPassword(string password) => _hasher.HashPassword(null!, password);

    public bool VerifyPassword(User user, string hashedPassword, string providedPassword) =>
        _hasher.VerifyHashedPassword(user, hashedPassword, providedPassword) is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
}

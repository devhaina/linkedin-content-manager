using LinkedInContentManager.Domain.Entities;

namespace LinkedInContentManager.Application.ExternalServices.Auth;

public interface IPasswordHasher
{
    string HashPassword(string password);

    bool VerifyPassword(User user, string hashedPassword, string providedPassword);
}

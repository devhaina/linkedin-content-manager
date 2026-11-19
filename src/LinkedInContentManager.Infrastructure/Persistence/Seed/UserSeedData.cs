using LinkedInContentManager.Domain.Entities;
using LinkedInContentManager.Domain.Enums;

namespace LinkedInContentManager.Infrastructure.Persistence.Seed;

/// <summary>
/// One seeded admin user so Campaign creation (which requires an existing <c>OwnerUserId</c>)
/// is testable before Phase 6 adds real registration/login. Its password hash is a
/// deliberately unusable placeholder, not a real hash — this user cannot log in until Phase 6
/// replaces it with a properly hashed password and wires actual authentication.
/// </summary>
public static class UserSeedData
{
    public static readonly Guid DefaultAdminUserId = Guid.Parse("22222222-0000-0000-0000-000000000001");

    public static IEnumerable<User> GetSeedUsers()
    {
        yield return new User(
            DefaultAdminUserId,
            "admin@linkedin-content-manager.local",
            "SEED$NOT_A_REAL_HASH$REPLACED_IN_PHASE_6",
            "Default Admin",
            UserRole.Admin);
    }
}

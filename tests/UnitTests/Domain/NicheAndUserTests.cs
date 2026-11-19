using FluentAssertions;
using LinkedInContentManager.Domain.Entities;
using LinkedInContentManager.Domain.Enums;

namespace LinkedInContentManager.UnitTests.Domain;

public class NicheTests
{
    [Fact]
    public void Constructor_WithEmptyName_Throws()
    {
        var act = () => new Niche("", "slug", "description");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Deactivate_Then_Activate_RoundTrips()
    {
        var niche = new Niche("Fintech", "fintech", "desc");

        niche.Deactivate();
        niche.IsActive.Should().BeFalse();

        niche.Activate();
        niche.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SeedConstructor_UsesFixedDeterministicTimestamp()
    {
        var a = new Niche(Guid.NewGuid(), "A", "a", "d");
        var b = new Niche(Guid.NewGuid(), "B", "b", "d");

        // Both seed rows must share the exact same instant — otherwise the EF Core model is
        // non-deterministic across builds (the real bug Phase 2 live testing caught).
        a.CreatedAtUtc.Should().Be(b.CreatedAtUtc).And.Be(Niche.SeedTimestamp);
    }
}

public class UserTests
{
    [Fact]
    public void Constructor_WithEmptyEmail_Throws()
    {
        var act = () => new User("", "hash", "Name", UserRole.Viewer);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetRefreshToken_Then_RevokeRefreshToken_ClearsIt()
    {
        var user = new User("a@b.com", "hash", "Name", UserRole.ContentManager);

        user.SetRefreshToken("hashedtoken", DateTimeOffset.UtcNow.AddDays(1));
        user.RefreshTokenHash.Should().Be("hashedtoken");

        user.RevokeRefreshToken();

        user.RefreshTokenHash.Should().BeNull();
        user.RefreshTokenExpiresAtUtc.Should().BeNull();
    }

    [Fact]
    public void ChangePassword_WithEmptyHash_Throws()
    {
        var user = new User("a@b.com", "hash", "Name", UserRole.ContentManager);

        var act = () => user.ChangePassword("");

        act.Should().Throw<ArgumentException>();
    }
}

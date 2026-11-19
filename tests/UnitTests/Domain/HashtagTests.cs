using FluentAssertions;
using LinkedInContentManager.Domain.Entities;

namespace LinkedInContentManager.UnitTests.Domain;

public class HashtagTests
{
    [Theory]
    [InlineData("#FintechSecurity", "fintechsecurity")]
    [InlineData("  CryptoNews  ", "cryptonews")]
    [InlineData("AI", "ai")]
    public void Normalize_StripsHashAndLowercases(string input, string expected)
    {
        Hashtag.Normalize(input).Should().Be(expected);
    }

    [Fact]
    public void Normalize_WithEmptyText_Throws()
    {
        var act = () => Hashtag.Normalize("   ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IncrementUsage_IncreasesCount()
    {
        var hashtag = new Hashtag("fintech");

        hashtag.IncrementUsage();
        hashtag.IncrementUsage();

        hashtag.UsageCount.Should().Be(2);
    }
}

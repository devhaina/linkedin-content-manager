using FluentAssertions;
using LinkedInContentManager.Domain.Entities;
using LinkedInContentManager.Domain.Enums;

namespace LinkedInContentManager.UnitTests.Domain;

public class ContentReviewTests
{
    [Fact]
    public void AddReview_ViaLinkedInPost_WithScoreAboveTen_Throws()
    {
        var post = new LinkedInPost(Guid.NewGuid(), WriterPersona.Practitioner, "draft");

        var act = () => post.AddReview(11, "a", "b", "c", "d", "e", [], []);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AddReview_ViaLinkedInPost_WithNegativeScore_Throws()
    {
        var post = new LinkedInPost(Guid.NewGuid(), WriterPersona.Practitioner, "draft");

        var act = () => post.AddReview(-1, "a", "b", "c", "d", "e", [], []);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AddReview_CapturesGuardrailViolations()
    {
        var post = new LinkedInPost(Guid.NewGuid(), WriterPersona.Practitioner, "draft");

        var review = post.AddReview(4, "weak", "weak", "low", "weak",
            "doesn't match persona", ["mentions a competitor by name"], ["remove the competitor mention"]);

        review.GuardrailViolations.Should().ContainSingle().Which.Should().Be("mentions a competitor by name");
    }
}

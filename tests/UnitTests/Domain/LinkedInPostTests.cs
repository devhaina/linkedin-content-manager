using FluentAssertions;
using LinkedInContentManager.Domain.Entities;
using LinkedInContentManager.Domain.Enums;

namespace LinkedInContentManager.UnitTests.Domain;

public class LinkedInPostTests
{
    [Fact]
    public void Constructor_ComputesDraftWordCount()
    {
        var post = new LinkedInPost(Guid.NewGuid(), WriterPersona.FormalAuthoritative, "one two three four five");

        post.DraftWordCount.Should().Be(5);
        post.Stage.Should().Be(PostStage.Draft);
    }

    [Fact]
    public void ApplyOptimization_SetsStageToOptimized_AndComputesWordCount()
    {
        var post = new LinkedInPost(Guid.NewGuid(), WriterPersona.Practitioner, "draft text here");

        post.ApplyOptimization("a much better optimized post with more words");

        post.Stage.Should().Be(PostStage.Optimized);
        post.OptimizedWordCount.Should().Be(8);
        post.EffectiveContent.Should().Be("a much better optimized post with more words");
    }

    [Fact]
    public void EffectiveContent_BeforeOptimization_FallsBackToDraft()
    {
        var post = new LinkedInPost(Guid.NewGuid(), WriterPersona.Practitioner, "draft only");

        post.EffectiveContent.Should().Be("draft only");
    }

    [Fact]
    public void AddReview_SetsStageToCritiqued_WhenNotYetOptimized()
    {
        var post = new LinkedInPost(Guid.NewGuid(), WriterPersona.Practitioner, "draft");

        post.AddReview(8, "strong", "good", "high", "clear", "fits persona", [], ["tighten the CTA"]);

        post.Stage.Should().Be(PostStage.Critiqued);
        post.Reviews.Should().ContainSingle().Which.Score.Should().Be(8);
    }

    [Fact]
    public void EditOptimizedContent_RecomputesWordCount()
    {
        var post = new LinkedInPost(Guid.NewGuid(), WriterPersona.Practitioner, "draft");
        post.ApplyOptimization("optimized content here");

        post.EditOptimizedContent("a human edited this down to four words");

        post.OptimizedWordCount.Should().Be(8);
    }

    [Fact]
    public void Constructor_WithEmptyDraft_Throws()
    {
        var act = () => new LinkedInPost(Guid.NewGuid(), WriterPersona.Practitioner, "");

        act.Should().Throw<ArgumentException>();
    }
}

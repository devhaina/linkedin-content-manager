using LinkedInContentManager.Domain.Common;

namespace LinkedInContentManager.Domain.Entities;

/// <summary>
/// Task 3 (Critique) output from the Content Critic agent. <see cref="GuardrailViolations"/>
/// is populated whenever the draft contains an unsourced claim, a competitor mention, or a
/// political/controversial topic — the Critic is required to flag these explicitly.
/// </summary>
public class ContentReview : BaseEntity
{
    public Guid LinkedInPostId { get; private set; }
    public LinkedInPost? LinkedInPost { get; private set; }

    public int Score { get; private set; }

    public string HookEvaluation { get; private set; } = string.Empty;

    public string StorytellingEvaluation { get; private set; } = string.Empty;

    public string EngagementPotential { get; private set; } = string.Empty;

    public string CtaEffectiveness { get; private set; } = string.Empty;

    /// <summary>Tone/readability assessment, including explicit fit with the post's selected persona.</summary>
    public string ToneReadabilityAssessment { get; private set; } = string.Empty;

    public List<string> GuardrailViolations { get; private set; } = [];

    public List<string> Recommendations { get; private set; } = [];

    private ContentReview() { }

    internal ContentReview(
        Guid linkedInPostId,
        int score,
        string hookEvaluation,
        string storytellingEvaluation,
        string engagementPotential,
        string ctaEffectiveness,
        string toneReadabilityAssessment,
        List<string> guardrailViolations,
        List<string> recommendations)
    {
        if (score is < 0 or > 10)
            throw new ArgumentOutOfRangeException(nameof(score), score, "Score must be between 0 and 10.");

        LinkedInPostId = linkedInPostId;
        Score = score;
        HookEvaluation = hookEvaluation ?? string.Empty;
        StorytellingEvaluation = storytellingEvaluation ?? string.Empty;
        EngagementPotential = engagementPotential ?? string.Empty;
        CtaEffectiveness = ctaEffectiveness ?? string.Empty;
        ToneReadabilityAssessment = toneReadabilityAssessment ?? string.Empty;
        GuardrailViolations = guardrailViolations ?? [];
        Recommendations = recommendations ?? [];
    }
}

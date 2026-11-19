using LinkedInContentManager.Domain.Common;
using LinkedInContentManager.Domain.Enums;

namespace LinkedInContentManager.Domain.Entities;

/// <summary>
/// The content artifact that flows through Writing → Critique → Optimization.
/// <see cref="DraftContent"/> is set once (Task 2, Content Writer) and never mutated —
/// <see cref="OptimizedContent"/> holds the publish-ready result of Task 4 (Optimizer).
/// </summary>
public class LinkedInPost : BaseEntity
{
    public Guid AgentExecutionId { get; private set; }
    public AgentExecution? AgentExecution { get; private set; }

    public WriterPersona Persona { get; private set; }

    public string DraftContent { get; private set; } = string.Empty;

    public int DraftWordCount { get; private set; }

    public string? OptimizedContent { get; private set; }

    public int? OptimizedWordCount { get; private set; }

    public PostStage Stage { get; private set; } = PostStage.Draft;

    private readonly List<LinkedInPostHashtag> _hashtags = [];
    public IReadOnlyCollection<LinkedInPostHashtag> Hashtags => _hashtags.AsReadOnly();

    private readonly List<ContentReview> _reviews = [];
    public IReadOnlyCollection<ContentReview> Reviews => _reviews.AsReadOnly();

    public ContentApproval? Approval { get; private set; }

    public PublishingSchedule? PublishingSchedule { get; private set; }

    private LinkedInPost() { }

    public LinkedInPost(Guid agentExecutionId, WriterPersona persona, string draftContent)
    {
        AgentExecutionId = agentExecutionId;
        Persona = persona;
        DraftContent = string.IsNullOrWhiteSpace(draftContent) ? throw new ArgumentException("DraftContent is required.", nameof(draftContent)) : draftContent;
        DraftWordCount = CountWords(draftContent);
    }

    public void AttachHashtag(Guid hashtagId, int position) => _hashtags.Add(new LinkedInPostHashtag(Id, hashtagId, position));

    public ContentReview AddReview(int score, string hookEvaluation, string storytellingEvaluation,
        string engagementPotential, string ctaEffectiveness, string toneReadabilityAssessment,
        List<string> guardrailViolations, List<string> recommendations)
    {
        var review = new ContentReview(Id, score, hookEvaluation, storytellingEvaluation,
            engagementPotential, ctaEffectiveness, toneReadabilityAssessment, guardrailViolations, recommendations);
        _reviews.Add(review);
        Stage = PostStage.Critiqued;
        Touch();
        return review;
    }

    public void ApplyOptimization(string optimizedContent)
    {
        OptimizedContent = string.IsNullOrWhiteSpace(optimizedContent)
            ? throw new ArgumentException("OptimizedContent is required.", nameof(optimizedContent))
            : optimizedContent;
        OptimizedWordCount = CountWords(optimizedContent);
        Stage = PostStage.Optimized;
        Touch();
    }

    /// <summary>The content ready to show for approval/editing/publishing — optimized if available, else the draft.</summary>
    public string EffectiveContent => OptimizedContent ?? DraftContent;

    public void SetApproval(ContentApproval approval)
    {
        if (Approval is not null)
            throw new Exceptions.InvalidStateTransitionException(nameof(LinkedInPost), "Approval already set", "new Approval");
        Approval = approval;
        Touch();
    }

    public void SetPublishingSchedule(PublishingSchedule schedule)
    {
        if (PublishingSchedule is not null)
            throw new Exceptions.InvalidStateTransitionException(nameof(LinkedInPost), "PublishingSchedule already set", "new PublishingSchedule");
        PublishingSchedule = schedule;
        Touch();
    }

    /// <summary>Lets an approver edit the final copy-paste-ready text directly (Content Approval § "edit" endpoint).</summary>
    public void EditOptimizedContent(string editedContent)
    {
        OptimizedContent = string.IsNullOrWhiteSpace(editedContent)
            ? throw new ArgumentException("EditedContent is required.", nameof(editedContent))
            : editedContent;
        OptimizedWordCount = CountWords(editedContent);
        Touch();
    }

    private static int CountWords(string text) =>
        text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
}

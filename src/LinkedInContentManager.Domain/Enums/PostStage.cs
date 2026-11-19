namespace LinkedInContentManager.Domain.Enums;

/// <summary>Which pipeline stage last produced/updated a <c>LinkedInPost</c>'s content.</summary>
public enum PostStage
{
    Draft = 0,
    Critiqued = 1,
    Optimized = 2
}

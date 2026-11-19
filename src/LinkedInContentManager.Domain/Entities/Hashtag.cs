using LinkedInContentManager.Domain.Common;

namespace LinkedInContentManager.Domain.Entities;

/// <summary>A normalized hashtag, reused across posts (unique on <see cref="Text"/>) so usage can be tracked.</summary>
public class Hashtag : BaseEntity
{
    /// <summary>Stored without the leading '#', lower-cased, e.g. "fintech".</summary>
    public string Text { get; private set; } = string.Empty;

    public int UsageCount { get; private set; }

    private readonly List<LinkedInPostHashtag> _postHashtags = [];
    public IReadOnlyCollection<LinkedInPostHashtag> PostHashtags => _postHashtags.AsReadOnly();

    private Hashtag() { }

    public Hashtag(string text)
    {
        Text = Normalize(text);
    }

    public static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Hashtag text is required.", nameof(text));
        return text.Trim().TrimStart('#').ToLowerInvariant();
    }

    public void IncrementUsage()
    {
        UsageCount++;
        Touch();
    }
}

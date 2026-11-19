using LinkedInContentManager.Domain.Common;

namespace LinkedInContentManager.Domain.Entities;

/// <summary>Join entity between <see cref="LinkedInPost"/> and <see cref="Hashtag"/>, preserving display order.</summary>
public class LinkedInPostHashtag : BaseEntity
{
    public Guid LinkedInPostId { get; private set; }
    public LinkedInPost? LinkedInPost { get; private set; }

    public Guid HashtagId { get; private set; }
    public Hashtag? Hashtag { get; private set; }

    public int Position { get; private set; }

    private LinkedInPostHashtag() { }

    public LinkedInPostHashtag(Guid linkedInPostId, Guid hashtagId, int position)
    {
        LinkedInPostId = linkedInPostId;
        HashtagId = hashtagId;
        Position = position;
    }
}

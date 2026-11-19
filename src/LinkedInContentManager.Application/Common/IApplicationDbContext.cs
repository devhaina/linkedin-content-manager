using LinkedInContentManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LinkedInContentManager.Application.Common;

/// <summary>
/// Persistence abstraction CQRS handlers depend on. Implemented by Infrastructure's
/// <c>ApplicationDbContext</c> — Application never references EF Core's runtime/provider
/// packages, only this contract and <see cref="DbSet{TEntity}"/>'s shape.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Niche> Niches { get; }
    DbSet<Campaign> Campaigns { get; }
    DbSet<ContentTopic> ContentTopics { get; }
    DbSet<AgentExecution> AgentExecutions { get; }
    DbSet<AgentExecutionLog> AgentExecutionLogs { get; }
    DbSet<ResearchResult> ResearchResults { get; }
    DbSet<LinkedInPost> LinkedInPosts { get; }
    DbSet<ContentReview> ContentReviews { get; }
    DbSet<ContentApproval> ContentApprovals { get; }
    DbSet<PublishingSchedule> PublishingSchedules { get; }
    DbSet<Hashtag> Hashtags { get; }
    DbSet<LinkedInPostHashtag> LinkedInPostHashtags { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

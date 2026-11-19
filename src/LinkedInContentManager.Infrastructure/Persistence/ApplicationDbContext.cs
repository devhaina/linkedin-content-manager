using LinkedInContentManager.Application.Common;
using LinkedInContentManager.Domain.Common;
using LinkedInContentManager.Domain.Entities;
using LinkedInContentManager.Infrastructure.Persistence.Seed;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LinkedInContentManager.Infrastructure.Persistence;

/// <summary>
/// The EF Core context backing every entity in the schema. Domain events raised by any
/// tracked <see cref="BaseEntity"/> are dispatched through MediatR immediately after a
/// successful <see cref="SaveChangesAsync"/> — "after", so a handler reacting to e.g.
/// <c>ContentApprovedDomainEvent</c> can trust the approval is already durable.
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IPublisher publisher)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Niche> Niches => Set<Niche>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<ContentTopic> ContentTopics => Set<ContentTopic>();
    public DbSet<AgentExecution> AgentExecutions => Set<AgentExecution>();
    public DbSet<AgentExecutionLog> AgentExecutionLogs => Set<AgentExecutionLog>();
    public DbSet<ResearchResult> ResearchResults => Set<ResearchResult>();
    public DbSet<LinkedInPost> LinkedInPosts => Set<LinkedInPost>();
    public DbSet<ContentReview> ContentReviews => Set<ContentReview>();
    public DbSet<ContentApproval> ContentApprovals => Set<ContentApproval>();
    public DbSet<PublishingSchedule> PublishingSchedules => Set<PublishingSchedule>();
    public DbSet<Hashtag> Hashtags => Set<Hashtag>();
    public DbSet<LinkedInPostHashtag> LinkedInPostHashtags => Set<LinkedInPostHashtag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        modelBuilder.Entity<Niche>().HasData(NicheSeedData.GetSeedNiches());
        modelBuilder.Entity<User>().HasData(UserSeedData.GetSeedUsers());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.Touch();
        }

        var domainEvents = ChangeTracker.Entries<BaseEntity>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            entry.Entity.ClearDomainEvents();

        foreach (var domainEvent in domainEvents)
            await PublishDomainEventAsync(domainEvent, cancellationToken);

        return result;
    }

    private Task PublishDomainEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
        var notification = Activator.CreateInstance(notificationType, domainEvent)!;
        return publisher.Publish(notification, cancellationToken);
    }
}

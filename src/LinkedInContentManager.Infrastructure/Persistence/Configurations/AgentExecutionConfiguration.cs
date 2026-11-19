using LinkedInContentManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkedInContentManager.Infrastructure.Persistence.Configurations;

public class AgentExecutionConfiguration : IEntityTypeConfiguration<AgentExecution>
{
    public void Configure(EntityTypeBuilder<AgentExecution> builder)
    {
        builder.ToTable("agent_executions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CorrelationId).HasMaxLength(100).IsRequired();
        builder.Property(e => e.ExternalExecutionId).HasMaxLength(100);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(e => e.ErrorDetails).HasMaxLength(4000);

        builder.HasMany(e => e.Logs)
            .WithOne(l => l.AgentExecution)
            .HasForeignKey(l => l.AgentExecutionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ResearchResult)
            .WithOne(r => r.AgentExecution)
            .HasForeignKey<ResearchResult>(r => r.AgentExecutionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.LinkedInPost)
            .WithOne(p => p.AgentExecution)
            .HasForeignKey<LinkedInPost>(p => p.AgentExecutionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.ContentTopicId);
        builder.HasIndex(e => e.CorrelationId);
        builder.HasIndex(e => e.ExternalExecutionId).IsUnique();
        builder.HasIndex(e => e.Status);
    }
}

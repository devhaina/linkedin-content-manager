using LinkedInContentManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkedInContentManager.Infrastructure.Persistence.Configurations;

public class AgentExecutionLogConfiguration : IEntityTypeConfiguration<AgentExecutionLog>
{
    public void Configure(EntityTypeBuilder<AgentExecutionLog> builder)
    {
        builder.ToTable("agent_execution_logs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Agent).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(l => l.TaskName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Message).HasMaxLength(4000).IsRequired();

        builder.HasIndex(l => l.AgentExecutionId);
        builder.HasIndex(l => l.Agent);
    }
}

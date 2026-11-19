using LinkedInContentManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkedInContentManager.Infrastructure.Persistence.Configurations;

public class ContentTopicConfiguration : IEntityTypeConfiguration<ContentTopic>
{
    public void Configure(EntityTypeBuilder<ContentTopic> builder)
    {
        builder.ToTable("content_topics");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Topic).HasMaxLength(500).IsRequired();
        builder.Property(t => t.PersonaOverride).HasConversion<string?>().HasMaxLength(50);

        builder.HasMany(t => t.AgentExecutions)
            .WithOne(e => e.ContentTopic)
            .HasForeignKey(e => e.ContentTopicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.CampaignId);
    }
}

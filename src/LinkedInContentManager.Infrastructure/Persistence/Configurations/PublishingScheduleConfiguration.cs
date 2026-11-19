using LinkedInContentManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkedInContentManager.Infrastructure.Persistence.Configurations;

public class PublishingScheduleConfiguration : IEntityTypeConfiguration<PublishingSchedule>
{
    public void Configure(EntityTypeBuilder<PublishingSchedule> builder)
    {
        builder.ToTable("publishing_schedules");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.RecommendedDay).HasMaxLength(20).IsRequired();
        builder.Property(s => s.RecommendedTime).HasMaxLength(20).IsRequired();
        builder.Property(s => s.AudienceTimezone).HasMaxLength(100).IsRequired();
        builder.Property(s => s.HashtagStrategy).HasColumnType("text");
        builder.Property(s => s.FirstHourEngagementPlan).HasColumnType("text");
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(s => s.LinkedInExternalPostId).HasMaxLength(100);
        builder.Property(s => s.PublishErrorDetails).HasMaxLength(4000);

        builder.HasIndex(s => s.LinkedInPostId).IsUnique();
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.LinkedInExternalPostId).IsUnique();
    }
}

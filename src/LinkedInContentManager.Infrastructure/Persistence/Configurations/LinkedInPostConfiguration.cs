using LinkedInContentManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkedInContentManager.Infrastructure.Persistence.Configurations;

public class LinkedInPostConfiguration : IEntityTypeConfiguration<LinkedInPost>
{
    public void Configure(EntityTypeBuilder<LinkedInPost> builder)
    {
        builder.ToTable("linkedin_posts");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Persona).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(p => p.DraftContent).HasColumnType("text").IsRequired();
        builder.Property(p => p.OptimizedContent).HasColumnType("text");
        builder.Property(p => p.Stage).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.HasMany(p => p.Reviews)
            .WithOne(r => r.LinkedInPost)
            .HasForeignKey(r => r.LinkedInPostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Hashtags)
            .WithOne(h => h.LinkedInPost)
            .HasForeignKey(h => h.LinkedInPostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Approval)
            .WithOne(a => a.LinkedInPost)
            .HasForeignKey<ContentApproval>(a => a.LinkedInPostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.PublishingSchedule)
            .WithOne(s => s.LinkedInPost)
            .HasForeignKey<PublishingSchedule>(s => s.LinkedInPostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.Stage);
    }
}

using LinkedInContentManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkedInContentManager.Infrastructure.Persistence.Configurations;

public class ContentApprovalConfiguration : IEntityTypeConfiguration<ContentApproval>
{
    public void Configure(EntityTypeBuilder<ContentApproval> builder)
    {
        builder.ToTable("content_approvals");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(a => a.RejectionReason).HasMaxLength(2000);

        builder.HasIndex(a => a.LinkedInPostId).IsUnique();
        builder.HasIndex(a => a.Status);
    }
}

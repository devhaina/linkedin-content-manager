using LinkedInContentManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkedInContentManager.Infrastructure.Persistence.Configurations;

public class HashtagConfiguration : IEntityTypeConfiguration<Hashtag>
{
    public void Configure(EntityTypeBuilder<Hashtag> builder)
    {
        builder.ToTable("hashtags");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Text).HasMaxLength(100).IsRequired();

        builder.HasIndex(h => h.Text).IsUnique();
    }
}

public class LinkedInPostHashtagConfiguration : IEntityTypeConfiguration<LinkedInPostHashtag>
{
    public void Configure(EntityTypeBuilder<LinkedInPostHashtag> builder)
    {
        builder.ToTable("linkedin_post_hashtags");

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Hashtag)
            .WithMany(h => h.PostHashtags)
            .HasForeignKey(x => x.HashtagId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.LinkedInPostId, x.HashtagId }).IsUnique();
    }
}

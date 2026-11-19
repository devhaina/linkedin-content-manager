using LinkedInContentManager.Domain.Entities;
using LinkedInContentManager.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkedInContentManager.Infrastructure.Persistence.Configurations;

public class ContentReviewConfiguration : IEntityTypeConfiguration<ContentReview>
{
    public void Configure(EntityTypeBuilder<ContentReview> builder)
    {
        builder.ToTable("content_reviews");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.HookEvaluation).HasColumnType("text");
        builder.Property(r => r.StorytellingEvaluation).HasColumnType("text");
        builder.Property(r => r.EngagementPotential).HasColumnType("text");
        builder.Property(r => r.CtaEffectiveness).HasColumnType("text");
        builder.Property(r => r.ToneReadabilityAssessment).HasColumnType("text");

        foreach (var propertyName in new[] { nameof(ContentReview.GuardrailViolations), nameof(ContentReview.Recommendations) })
        {
            builder.Property<List<string>>(propertyName)
                .HasConversion(new JsonStringListConverter())
                .Metadata.SetValueComparer(JsonStringListConverter.Comparer);
        }

        builder.HasIndex(r => r.LinkedInPostId);
        builder.HasIndex(r => r.Score);
    }
}

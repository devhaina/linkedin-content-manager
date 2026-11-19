using LinkedInContentManager.Domain.Entities;
using LinkedInContentManager.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkedInContentManager.Infrastructure.Persistence.Configurations;

public class ResearchResultConfiguration : IEntityTypeConfiguration<ResearchResult>
{
    public void Configure(EntityTypeBuilder<ResearchResult> builder)
    {
        builder.ToTable("research_results");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.RawOutput).HasColumnType("text");

        foreach (var propertyName in new[]
        {
            nameof(ResearchResult.TrendingContentAngles),
            nameof(ResearchResult.TrendingHashtags),
            nameof(ResearchResult.TrendingHooks),
            nameof(ResearchResult.SourceUrls)
        })
        {
            builder.Property<List<string>>(propertyName)
                .HasConversion(new JsonStringListConverter())
                .Metadata.SetValueComparer(JsonStringListConverter.Comparer);
        }
    }
}

using LinkedInContentManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkedInContentManager.Infrastructure.Persistence.Configurations;

public class NicheConfiguration : IEntityTypeConfiguration<Niche>
{
    public void Configure(EntityTypeBuilder<Niche> builder)
    {
        builder.ToTable("niches");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Name).HasMaxLength(100).IsRequired();
        builder.Property(n => n.Slug).HasMaxLength(100).IsRequired();
        builder.Property(n => n.Description).HasMaxLength(500);

        builder.HasIndex(n => n.Slug).IsUnique();

        builder.HasMany(n => n.Campaigns)
            .WithOne(c => c.Niche)
            .HasForeignKey(c => c.NicheId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Congress.CongressReviewer;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class CongressReviewerConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("CongressReviewers");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("Id")
            .IsRequired();

        builder.Property(entity => entity.CongressId)
            .IsRequired();

        builder.Property(entity => entity.ReviewerId)
            .IsRequired();

        builder.Property(entity => entity.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(entity => entity.ExpertiseKeywords)
            .HasMaxLength(500);

        builder.Property(entity => entity.Note)
            .HasMaxLength(1000);

        builder.HasIndex(entity => new { entity.CongressId, entity.ReviewerId })
            .IsUnique()
            .HasFilter("\"DeletedDate\" IS NULL");

        builder.HasIndex(entity => entity.CongressId);
        builder.HasIndex(entity => entity.ReviewerId);
        builder.HasIndex(entity => entity.IsActive);

        builder.HasOne(entity => entity.Congress)
            .WithMany()
            .HasForeignKey(entity => entity.CongressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.Reviewer)
            .WithMany(reviewer => reviewer.CongressReviewers)
            .HasForeignKey(entity => entity.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class SubmissionFileConfiguration : IEntityTypeConfiguration<SubmissionFile>
{
    public void Configure(EntityTypeBuilder<SubmissionFile> builder)
    {
        builder.ToTable("SubmissionFiles");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.FileKind).HasConversion<int>().IsRequired();
        builder.Property(entity => entity.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(entity => entity.FilePath).HasMaxLength(750).IsRequired();
        builder.Property(entity => entity.ContentType).HasMaxLength(150);
        builder.Property(entity => entity.ReviewStatus).HasConversion<int>().IsRequired().HasDefaultValue(SubmissionFileReviewStatus.PendingReview);
        builder.Property(entity => entity.ReviewNote).HasMaxLength(1000);
        builder.Property(entity => entity.IsIncludedInProgramBook).IsRequired().HasDefaultValue(false);
        builder.Property(entity => entity.VersionNo).IsRequired().HasDefaultValue(1);
        builder.Property(entity => entity.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasOne(entity => entity.Submission)
            .WithMany(submission => submission.Files)
            .HasForeignKey(entity => entity.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(entity => new { entity.SubmissionId, entity.FileKind })
            .HasFilter("\"DeletedDate\" IS NULL AND \"IsActive\"");

        builder.HasIndex(entity => new { entity.FileKind, entity.ReviewStatus, entity.IsActive })
            .HasFilter("\"DeletedDate\" IS NULL");

        builder.HasQueryFilter(entity => entity.DeletedDate == null && entity.Submission.DeletedDate == null);
    }
}

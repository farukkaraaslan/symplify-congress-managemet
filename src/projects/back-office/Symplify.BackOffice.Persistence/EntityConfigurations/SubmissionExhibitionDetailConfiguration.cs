using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class SubmissionExhibitionDetailConfiguration : IEntityTypeConfiguration<SubmissionExhibitionDetail>
{
    public void Configure(EntityTypeBuilder<SubmissionExhibitionDetail> builder)
    {
        builder.ToTable("SubmissionExhibitionDetails");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.WorkName)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(entity => entity.Dimensions)
            .HasMaxLength(200);

        builder.Property(entity => entity.Technique)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(entity => entity.Description)
            .HasMaxLength(4000);

        builder.Property(entity => entity.Address)
            .HasMaxLength(1000)
            .IsRequired();

        builder.HasOne(entity => entity.Submission)
            .WithOne(submission => submission.ExhibitionDetail)
            .HasForeignKey<SubmissionExhibitionDetail>(entity => entity.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(entity => entity.SubmissionId)
            .IsUnique()
            .HasFilter("\"DeletedDate\" IS NULL");

        builder.HasQueryFilter(entity => entity.DeletedDate == null && entity.Submission.DeletedDate == null);
    }
}

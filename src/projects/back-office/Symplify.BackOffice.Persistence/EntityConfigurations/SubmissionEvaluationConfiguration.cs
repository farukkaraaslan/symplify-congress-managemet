using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Submission.SubmissionEvaluation;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class SubmissionEvaluationConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("SubmissionEvaluations");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasColumnName("Id").IsRequired();

        builder.Property(entity => entity.Comment).HasMaxLength(4000);
        builder.Property(entity => entity.EditorComment).HasMaxLength(4000);
        builder.Property(entity => entity.Recommendation).HasMaxLength(200);
        builder.Property(entity => entity.TotalScore).HasPrecision(18, 2);

        builder.HasOne(entity => entity.Submission)
            .WithMany(submission => submission.Evaluations)
            .HasForeignKey(entity => entity.SubmissionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.Reviewer)
            .WithMany(reviewer => reviewer.Evaluations)
            .HasForeignKey(entity => entity.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => new { entity.SubmissionId, entity.ReviewerId })
            .IsUnique()
            .HasFilter("\"DeletedDate\" IS NULL");
    }
}

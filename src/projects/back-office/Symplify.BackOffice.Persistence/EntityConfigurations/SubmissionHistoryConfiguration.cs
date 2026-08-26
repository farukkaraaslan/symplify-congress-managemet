using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Submission.SubmissionHistory;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class SubmissionHistoryConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("SubmissionHistories");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Note).HasMaxLength(1000);
        builder.Property(entity => entity.PublicNote).HasMaxLength(2000);
        builder.Property(entity => entity.InternalNote).HasMaxLength(2000);
        builder.Property(entity => entity.PerformedAt).IsRequired();
        builder.Property(entity => entity.IsAutomatic).IsRequired().HasDefaultValue(false);

        builder.HasOne(entity => entity.Submission)
            .WithMany(submission => submission.Histories)
            .HasForeignKey(entity => entity.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entity => entity.FromStatus)
            .WithMany()
            .HasForeignKey(entity => entity.FromStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.ToStatus)
            .WithMany()
            .HasForeignKey(entity => entity.ToStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.PerformedByUser)
            .WithMany()
            .HasForeignKey(entity => entity.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.TransactionStatusTransition)
            .WithMany()
            .HasForeignKey(entity => entity.TransactionStatusTransitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => new { entity.SubmissionId, entity.PerformedAt });
        builder.HasQueryFilter(entity => entity.DeletedDate == null && entity.Submission.DeletedDate == null);
    }
}

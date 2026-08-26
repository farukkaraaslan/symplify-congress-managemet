using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class ParticipationCertificateGenerationJobConfiguration : IEntityTypeConfiguration<ParticipationCertificateGenerationJob>
{
    public void Configure(EntityTypeBuilder<ParticipationCertificateGenerationJob> builder)
    {
        builder.ToTable("ParticipationCertificateGenerationJobs");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Culture).HasMaxLength(10).IsRequired();
        builder.Property(entity => entity.SubmissionStatusCode).HasMaxLength(100);
        builder.Property(entity => entity.PaymentStatusCode).HasMaxLength(100);
        builder.Property(entity => entity.CandidateSearch).HasMaxLength(500);
        builder.Property(entity => entity.SelectedCandidateKeysJson).HasColumnType("text").IsRequired();
        builder.Property(entity => entity.ExcludedCandidateKeysJson).HasColumnType("text").IsRequired();
        builder.Property(entity => entity.LastError).HasMaxLength(2000);
        builder.HasOne(entity => entity.Congress)
            .WithMany()
            .HasForeignKey(entity => entity.CongressId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(entity => entity.Items)
            .WithOne(entity => entity.Job)
            .HasForeignKey(entity => entity.JobId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(entity => new { entity.Status, entity.CreatedDate });
        builder.HasIndex(entity => new { entity.CongressId, entity.Culture, entity.Status });
        builder.HasQueryFilter(entity => entity.DeletedDate == null);
    }
}

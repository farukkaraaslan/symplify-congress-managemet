using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class ParticipationCertificateGenerationJobItemConfiguration : IEntityTypeConfiguration<ParticipationCertificateGenerationJobItem>
{
    public void Configure(EntityTypeBuilder<ParticipationCertificateGenerationJobItem> builder)
    {
        builder.ToTable("ParticipationCertificateGenerationJobItems");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.SubmissionNumber).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.SubmissionTitle).HasMaxLength(600).IsRequired();
        builder.Property(entity => entity.SubmissionTypeName).HasMaxLength(250).IsRequired();
        builder.Property(entity => entity.AuthorDisplayName).HasMaxLength(250).IsRequired();
        builder.Property(entity => entity.AuthorEmail).HasMaxLength(250);
        builder.Property(entity => entity.AuthorInstitution).HasMaxLength(500);
        builder.Property(entity => entity.LastError).HasMaxLength(2000);
        builder.HasIndex(entity => new { entity.JobId, entity.SubmissionId, entity.AuthorId }).IsUnique();
        builder.HasIndex(entity => new { entity.JobId, entity.Status, entity.Id });
        builder.HasQueryFilter(entity => entity.DeletedDate == null && entity.Job.DeletedDate == null);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class ParticipationCertificateConfiguration : IEntityTypeConfiguration<ParticipationCertificate>
{
    public void Configure(EntityTypeBuilder<ParticipationCertificate> builder)
    {
        builder.ToTable("ParticipationCertificates");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Culture).HasMaxLength(10).IsRequired();
        builder.Property(entity => entity.SubmissionNumber).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.SubmissionTitleSnapshot).HasMaxLength(600).IsRequired();
        builder.Property(entity => entity.AuthorFullNameSnapshot).HasMaxLength(250).IsRequired();
        builder.Property(entity => entity.AuthorEmailSnapshot).HasMaxLength(250);
        builder.Property(entity => entity.AuthorInstitutionSnapshot).HasMaxLength(500);
        builder.Property(entity => entity.FileName).HasMaxLength(255).IsRequired();
        builder.Property(entity => entity.StorageProvider).HasMaxLength(100);
        builder.Property(entity => entity.BucketName).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.ObjectName).HasMaxLength(1000).IsRequired();
        builder.Property(entity => entity.ContentType).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.ETag).HasMaxLength(250);
        builder.Property(entity => entity.EmailStatus).HasMaxLength(50);
        builder.Property(entity => entity.EmailError).HasMaxLength(1000);
        builder.Property(entity => entity.PublicAccessTokenHash).HasMaxLength(128);
        builder.Property(entity => entity.RevocationReason).HasMaxLength(1000);
        builder.Property(entity => entity.GeneratedAt).IsRequired();

        builder.HasOne(entity => entity.Congress)
            .WithMany()
            .HasForeignKey(entity => entity.CongressId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entity => entity.Submission)
            .WithMany()
            .HasForeignKey(entity => entity.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entity => entity.Author)
            .WithMany()
            .HasForeignKey(entity => entity.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.Template)
            .WithMany()
            .HasForeignKey(entity => entity.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => new { entity.CongressId, entity.SubmissionId, entity.AuthorId, entity.Culture })
            .IsUnique()
            .HasFilter("\"DeletedDate\" IS NULL");

        builder.HasIndex(entity => new { entity.CongressId, entity.EmailStatus })
            .HasFilter("\"DeletedDate\" IS NULL");

        builder.HasIndex(entity => entity.PublicId)
            .IsUnique()
            .HasFilter("\"PublicId\" IS NOT NULL");

        builder.HasIndex(entity => new { entity.CongressId, entity.PublishedAt, entity.RevokedAt });

        builder.HasQueryFilter(entity => entity.DeletedDate == null && entity.Submission.DeletedDate == null);
    }
}

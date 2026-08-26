using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class ParticipationCertificateTemplateConfiguration : IEntityTypeConfiguration<ParticipationCertificateTemplate>
{
    public void Configure(EntityTypeBuilder<ParticipationCertificateTemplate> builder)
    {
        builder.ToTable("ParticipationCertificateTemplates");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Culture).HasMaxLength(10).IsRequired();
        builder.Property(entity => entity.BodyText).HasMaxLength(4000);
        builder.Property(entity => entity.MailSubject).HasMaxLength(300);
        builder.Property(entity => entity.MailTitle).HasMaxLength(300);
        builder.Property(entity => entity.MailBodyHtml).HasColumnType("text");
        builder.Property(entity => entity.StorageProvider).HasMaxLength(100);
        builder.Property(entity => entity.BucketName).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.ObjectName).HasMaxLength(1000).IsRequired();
        builder.Property(entity => entity.FileName).HasMaxLength(255).IsRequired();
        builder.Property(entity => entity.ContentType).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.ETag).HasMaxLength(250);
        builder.Property(entity => entity.NameFontColorHex).HasMaxLength(20).IsRequired();
        builder.Property(entity => entity.PlaceholderBackgroundColorHex).HasMaxLength(20).IsRequired();

        builder.HasOne(entity => entity.Congress)
            .WithMany()
            .HasForeignKey(entity => entity.CongressId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(entity => new { entity.CongressId, entity.Culture, entity.IsActive })
            .HasFilter("\"DeletedDate\" IS NULL");

        builder.HasIndex(entity => new { entity.CongressId, entity.Culture })
            .HasDatabaseName("UX_ParticipationCertificateTemplates_CongressId_Culture_Active")
            .IsUnique()
            .HasFilter("\"DeletedDate\" IS NULL AND \"IsActive\" = TRUE");

        builder.HasIndex(entity => entity.CongressId)
            .HasDatabaseName("UX_ParticipationCertificateTemplates_CongressId_Default")
            .IsUnique()
            .HasFilter("\"DeletedDate\" IS NULL AND \"IsActive\" = TRUE AND \"IsDefault\" = TRUE");

        builder.HasQueryFilter(entity => entity.DeletedDate == null && entity.Congress.DeletedDate == null);
    }
}

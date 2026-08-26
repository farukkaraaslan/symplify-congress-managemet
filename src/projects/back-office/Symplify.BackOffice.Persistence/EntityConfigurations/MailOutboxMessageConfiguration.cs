using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Symplify.BackOffice.Domain.Communication;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class MailOutboxMessageConfiguration : IEntityTypeConfiguration<MailOutboxMessage>
{
    public void Configure(EntityTypeBuilder<MailOutboxMessage> builder)
    {
        builder.ToTable("MailOutboxMessages");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.ToEmail).HasMaxLength(250).IsRequired();
        builder.Property(entity => entity.ToName).HasMaxLength(250);
        builder.Property(entity => entity.Subject).HasMaxLength(300).IsRequired();
        builder.Property(entity => entity.HtmlBody).IsRequired();
        builder.Property(entity => entity.MailType).HasConversion<int>().HasDefaultValue(MailMessageType.Unknown).IsRequired();
        builder.Property(entity => entity.FromEmail).HasMaxLength(250);
        builder.Property(entity => entity.FromName).HasMaxLength(250);
        builder.Property(entity => entity.ReplyToEmail).HasMaxLength(250);
        builder.Property(entity => entity.ReplyToName).HasMaxLength(250);
        builder.Property(entity => entity.AttachmentPath).HasMaxLength(750);
        builder.Property(entity => entity.AttachmentBucketName).HasMaxLength(150);
        builder.Property(entity => entity.AttachmentObjectName).HasMaxLength(750);
        builder.Property(entity => entity.AttachmentFileName).HasMaxLength(260);
        builder.Property(entity => entity.AttachmentContentType).HasMaxLength(150);
        builder.Property(entity => entity.Status).HasConversion<int>().IsRequired();
        builder.Property(entity => entity.LastError).HasMaxLength(1000);
        builder.Property(entity => entity.DeliveryStatus).HasConversion<int>().HasDefaultValue(MailDeliveryStatus.Unknown).IsRequired();
        builder.Property(entity => entity.Provider).HasMaxLength(50);
        builder.Property(entity => entity.ProviderMessageId).HasMaxLength(200);
        builder.Property(entity => entity.DeliveryStatusCode).HasMaxLength(100);
        builder.Property(entity => entity.DeliveryDiagnosticCode).HasMaxLength(2000);
        builder.Property(entity => entity.DeliverySmtpResponse).HasMaxLength(2000);
        builder.Property(entity => entity.BounceType).HasMaxLength(100);
        builder.Property(entity => entity.BounceSubType).HasMaxLength(100);
        builder.Property(entity => entity.ContainsSensitiveContent).HasDefaultValue(false).IsRequired();
        builder.Property(entity => entity.BulkEmailCulture).HasMaxLength(15);
        builder.Property(entity => entity.OpenCount).HasDefaultValue(0).IsRequired();

        builder.HasMany(entity => entity.DeliveryEvents)
            .WithOne(entity => entity.MailOutboxMessage)
            .HasForeignKey(entity => entity.MailOutboxMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(entity => new { entity.Status, entity.CreatedDate });
        builder.HasIndex(entity => new { entity.DeliveryStatus, entity.CreatedDate });
        builder.HasIndex(entity => new { entity.MailType, entity.CreatedDate });
        builder.HasIndex(entity => entity.RelatedUserId);
        builder.HasIndex(entity => entity.RelatedAuthorId);
        builder.HasIndex(entity => entity.RelatedSubmissionId);
        builder.HasIndex(entity => entity.AcceptanceLetterId);
        builder.HasIndex(entity => entity.ParticipationCertificateId);
        builder.HasIndex(entity => entity.BulkEmailBatchId);
        builder.HasIndex(entity => entity.ProviderMessageId);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.CreatedDate });
        builder.HasIndex(entity => new { entity.CongressId, entity.CreatedDate });
        builder.HasIndex(entity => entity.TrackingToken)
            .IsUnique()
            .HasFilter("\"TrackingToken\" IS NOT NULL");

        builder.HasQueryFilter(entity => entity.DeletedDate == null);
    }
}

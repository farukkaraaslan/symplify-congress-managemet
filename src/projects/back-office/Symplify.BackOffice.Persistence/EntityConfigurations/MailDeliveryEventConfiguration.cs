using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Symplify.BackOffice.Domain.Communication;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class MailDeliveryEventConfiguration : IEntityTypeConfiguration<MailDeliveryEvent>
{
    public void Configure(EntityTypeBuilder<MailDeliveryEvent> builder)
    {
        builder.ToTable("MailDeliveryEvents");

        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.ProviderEventId).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.ProviderMessageId).HasMaxLength(200);
        builder.Property(entity => entity.EventType).HasConversion<int>().IsRequired();
        builder.Property(entity => entity.StatusCode).HasMaxLength(100);
        builder.Property(entity => entity.DiagnosticCode).HasMaxLength(2000);
        builder.Property(entity => entity.BounceType).HasMaxLength(100);
        builder.Property(entity => entity.BounceSubType).HasMaxLength(100);
        builder.Property(entity => entity.SmtpResponse).HasMaxLength(2000);
        builder.Property(entity => entity.Detail).HasMaxLength(2000);

        builder.HasIndex(entity => entity.ProviderEventId).IsUnique();
        builder.HasIndex(entity => new { entity.MailOutboxMessageId, entity.OccurredAt });
        builder.HasIndex(entity => entity.ProviderMessageId);

        builder.HasQueryFilter(entity => entity.DeletedDate == null);
    }
}

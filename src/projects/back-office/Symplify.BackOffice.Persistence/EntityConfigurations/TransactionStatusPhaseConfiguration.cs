using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class TransactionStatusPhaseConfiguration : IEntityTypeConfiguration<TransactionStatusPhase>
{
    public void Configure(EntityTypeBuilder<TransactionStatusPhase> builder)
    {
        builder.ToTable("TransactionStatusPhases");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Order)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedDate)
            .IsRequired();

        builder.Property(x => x.UpdatedDate)
            .IsRequired(false);

        builder.Property(x => x.DeletedDate)
            .IsRequired(false);

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.DeletedBy)
            .HasMaxLength(100);

        builder.HasQueryFilter(x => x.DeletedDate == null);

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("\"DeletedDate\" IS NULL");

        builder.HasIndex(x => x.Order)
            .HasFilter("\"DeletedDate\" IS NULL");

        builder.HasMany(x => x.Translations)
            .WithOne(x => x.TransactionStatusPhase)
            .HasForeignKey(x => x.TransactionStatusPhaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.TransactionStatuses)
            .WithOne(x => x.TransactionStatusPhase)
            .HasForeignKey(x => x.TransactionStatusPhaseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
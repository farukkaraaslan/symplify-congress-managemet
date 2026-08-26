using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class TransactionStatusPhaseTranslationConfiguration
    : IEntityTypeConfiguration<TransactionStatusPhaseTranslation>
{
    public void Configure(EntityTypeBuilder<TransactionStatusPhaseTranslation> builder)
    {
        builder.ToTable("TransactionStatusPhaseTranslations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TransactionStatusPhaseId)
            .IsRequired();

        builder.Property(x => x.LanguageId)
            .IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .IsRequired(false)
            .HasMaxLength(1000);

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

        builder.HasIndex(x => new
        {
            x.TransactionStatusPhaseId,
            x.LanguageId
        })
            .IsUnique()
            .HasFilter("\"DeletedDate\" IS NULL");

        builder.HasOne(x => x.TransactionStatusPhase)
            .WithMany(x => x.Translations)
            .HasForeignKey(x => x.TransactionStatusPhaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Language)
            .WithMany()
            .HasForeignKey(x => x.LanguageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Workflow.TransactionStatusTransitionTranslation;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class TransactionStatusTransitionTranslationConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("TransactionStatusTransitionTranslations");

        builder.HasKey(entity => entity.Id);

        ConfigureProperties(builder);
        ConfigureRelationships(builder);
        ConfigureIndexes(builder);
        ConfigureQueryFilters(builder);
    }

    private static void ConfigureProperties(EntityTypeBuilder<EntityType> builder)
    {
        builder.Property(entity => entity.Id)
            .HasColumnName("Id")
            .IsRequired();

        builder.Property(entity => entity.TransactionStatusTransitionId)
            .HasColumnName("TransactionStatusTransitionId")
            .IsRequired();

        builder.Property(entity => entity.LanguageId)
            .HasColumnName("LanguageId")
            .IsRequired();

        builder.Property(entity => entity.Name)
            .HasColumnName("Name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(entity => entity.Description)
            .HasColumnName("Description")
            .HasMaxLength(1000)
            .IsRequired(false);
    }

    private static void ConfigureRelationships(EntityTypeBuilder<EntityType> builder)
    {
        builder.HasOne(entity => entity.TransactionStatusTransition)
            .WithMany(transition => transition.Translations)
            .HasForeignKey(entity => entity.TransactionStatusTransitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entity => entity.Language)
            .WithMany()
            .HasForeignKey(entity => entity.LanguageId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureIndexes(EntityTypeBuilder<EntityType> builder)
    {
        builder.HasIndex(entity => new
        {
            entity.TransactionStatusTransitionId,
            entity.LanguageId
        })
            .IsUnique()
            .HasFilter("\"DeletedDate\" IS NULL");
    }

    private static void ConfigureQueryFilters(EntityTypeBuilder<EntityType> builder)
    {
        builder.HasQueryFilter(entity =>
            entity.DeletedDate == null &&
            entity.TransactionStatusTransition.DeletedDate == null &&
            entity.TransactionStatusTransition.FromStatus.DeletedDate == null &&
            entity.TransactionStatusTransition.FromStatus.TransactionStatusPhase.DeletedDate == null &&
            entity.TransactionStatusTransition.ToStatus.DeletedDate == null &&
            entity.TransactionStatusTransition.ToStatus.TransactionStatusPhase.DeletedDate == null);
    }
}
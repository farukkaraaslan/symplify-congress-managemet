using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Workflow.TransactionStatusTranslation;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class TransactionStatusTranslationConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("TransactionStatusTranslations");

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

        builder.Property(entity => entity.TransactionStatusId)
            .HasColumnName("TransactionStatusId")
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
        builder.HasOne(entity => entity.TransactionStatus)
            .WithMany(status => status.Translations)
            .HasForeignKey(entity => entity.TransactionStatusId)
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
            entity.TransactionStatusId,
            entity.LanguageId
        })
            .IsUnique()
            .HasFilter("\"DeletedDate\" IS NULL");
    }

    private static void ConfigureQueryFilters(EntityTypeBuilder<EntityType> builder)
    {
        builder.HasQueryFilter(entity =>
            entity.DeletedDate == null &&
            entity.TransactionStatus.DeletedDate == null &&
            entity.TransactionStatus.TransactionStatusPhase.DeletedDate == null);
    }
}
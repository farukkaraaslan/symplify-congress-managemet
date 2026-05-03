using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Workflow.TransactionStatus;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class TransactionStatusConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("TransactionStatuses");

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

        builder.Property(entity => entity.TransactionStatusPhaseId)
            .HasColumnName("TransactionStatusPhaseId")
            .IsRequired();

        builder.Property(entity => entity.Code)
            .HasColumnName("Code")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(entity => entity.Order)
            .HasColumnName("Order")
            .IsRequired();

        builder.Property(entity => entity.IsEditable)
            .HasColumnName("IsEditable")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(entity => entity.IsFinal)
            .HasColumnName("IsFinal")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(entity => entity.IsActive)
            .HasColumnName("IsActive")
            .IsRequired();
    }

    private static void ConfigureRelationships(EntityTypeBuilder<EntityType> builder)
    {
        builder.HasOne(entity => entity.TransactionStatusPhase)
            .WithMany(phase => phase.TransactionStatuses)
            .HasForeignKey(entity => entity.TransactionStatusPhaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(entity => entity.Translations)
            .WithOne(translation => translation.TransactionStatus)
            .HasForeignKey(translation => translation.TransactionStatusId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(entity => entity.FromTransitions)
            .WithOne(transition => transition.FromStatus)
            .HasForeignKey(transition => transition.FromStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(entity => entity.ToTransitions)
            .WithOne(transition => transition.ToStatus)
            .HasForeignKey(transition => transition.ToStatusId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureIndexes(EntityTypeBuilder<EntityType> builder)
    {
        builder.HasIndex(entity => entity.Code)
            .IsUnique()
            .HasFilter("\"DeletedDate\" IS NULL");

        builder.HasIndex(entity => new
        {
            entity.TransactionStatusPhaseId,
            entity.Order
        })
            .HasFilter("\"DeletedDate\" IS NULL");
    }

    private static void ConfigureQueryFilters(EntityTypeBuilder<EntityType> builder)
    {
        builder.HasQueryFilter(entity =>
            entity.DeletedDate == null &&
            entity.TransactionStatusPhase.DeletedDate == null);
    }
}
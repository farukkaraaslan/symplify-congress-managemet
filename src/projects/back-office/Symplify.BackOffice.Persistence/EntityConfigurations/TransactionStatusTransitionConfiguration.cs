using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Workflow.TransactionStatusTransition;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class TransactionStatusTransitionConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("TransactionStatusTransitions");

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

        builder.Property(entity => entity.FromStatusId)
            .HasColumnName("FromStatusId")
            .IsRequired();

        builder.Property(entity => entity.ToStatusId)
            .HasColumnName("ToStatusId")
            .IsRequired();

        builder.Property(entity => entity.IsActive)
            .HasColumnName("IsActive")
            .IsRequired()
            .HasDefaultValue(true);
    }

    private static void ConfigureRelationships(EntityTypeBuilder<EntityType> builder)
    {
        builder.HasOne(entity => entity.FromStatus)
            .WithMany(status => status.FromTransitions)
            .HasForeignKey(entity => entity.FromStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.ToStatus)
            .WithMany(status => status.ToTransitions)
            .HasForeignKey(entity => entity.ToStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(entity => entity.Translations)
            .WithOne(translation => translation.TransactionStatusTransition)
            .HasForeignKey(translation => translation.TransactionStatusTransitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureIndexes(EntityTypeBuilder<EntityType> builder)
    {
        builder.HasIndex(entity => new
        {
            entity.FromStatusId,
            entity.ToStatusId
        })
            .IsUnique()
            .HasFilter("\"DeletedDate\" IS NULL");
    }

    private static void ConfigureQueryFilters(EntityTypeBuilder<EntityType> builder)
    {
        builder.HasQueryFilter(entity =>
            entity.DeletedDate == null &&
            entity.FromStatus.DeletedDate == null &&
            entity.FromStatus.TransactionStatusPhase.DeletedDate == null &&
            entity.ToStatus.DeletedDate == null &&
            entity.ToStatus.TransactionStatusPhase.DeletedDate == null);
    }
}
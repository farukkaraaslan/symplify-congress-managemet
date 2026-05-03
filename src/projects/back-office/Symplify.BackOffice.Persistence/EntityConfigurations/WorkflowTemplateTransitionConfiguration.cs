using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Workflow.WorkflowTemplateTransition;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class WorkflowTemplateTransitionConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("WorkflowTemplateTransitions");

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

        builder.Property(entity => entity.WorkflowTemplateId)
            .HasColumnName("WorkflowTemplateId")
            .IsRequired();

        builder.Property(entity => entity.TransactionStatusTransitionId)
            .HasColumnName("TransactionStatusTransitionId")
            .IsRequired();

        builder.Property(entity => entity.Order)
            .HasColumnName("Order")
            .IsRequired();

        builder.Property(entity => entity.IsActive)
            .HasColumnName("IsActive")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(entity => entity.CreatedDate)
            .HasColumnName("CreatedDate")
            .IsRequired();

        builder.Property(entity => entity.UpdatedDate)
            .HasColumnName("UpdatedDate")
            .IsRequired(false);

        builder.Property(entity => entity.DeletedDate)
            .HasColumnName("DeletedDate")
            .IsRequired(false);

        builder.Property(entity => entity.CreatedBy)
            .HasColumnName("CreatedBy")
            .HasMaxLength(100);

        builder.Property(entity => entity.UpdatedBy)
            .HasColumnName("UpdatedBy")
            .HasMaxLength(100);

        builder.Property(entity => entity.DeletedBy)
            .HasColumnName("DeletedBy")
            .HasMaxLength(100);
    }

    private static void ConfigureRelationships(EntityTypeBuilder<EntityType> builder)
    {
        builder.HasOne(entity => entity.WorkflowTemplate)
            .WithMany(template => template.Transitions)
            .HasForeignKey(entity => entity.WorkflowTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entity => entity.TransactionStatusTransition)
            .WithMany(transition => transition.WorkflowTemplateTransitions)
            .HasForeignKey(entity => entity.TransactionStatusTransitionId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureIndexes(EntityTypeBuilder<EntityType> builder)
    {
        builder.HasIndex(entity => new
            {
                entity.WorkflowTemplateId,
                entity.TransactionStatusTransitionId
            })
            .IsUnique()
            .HasFilter("\"DeletedDate\" IS NULL");

        builder.HasIndex(entity => new
            {
                entity.WorkflowTemplateId,
                entity.Order
            })
            .HasFilter("\"DeletedDate\" IS NULL");
    }

    private static void ConfigureQueryFilters(EntityTypeBuilder<EntityType> builder)
    {
        builder.HasQueryFilter(entity =>
            entity.DeletedDate == null &&
            entity.WorkflowTemplate.DeletedDate == null &&
            entity.TransactionStatusTransition.DeletedDate == null &&
            entity.TransactionStatusTransition.FromStatus.DeletedDate == null &&
            entity.TransactionStatusTransition.FromStatus.TransactionStatusPhase.DeletedDate == null &&
            entity.TransactionStatusTransition.ToStatus.DeletedDate == null &&
            entity.TransactionStatusTransition.ToStatus.TransactionStatusPhase.DeletedDate == null);
    }
}

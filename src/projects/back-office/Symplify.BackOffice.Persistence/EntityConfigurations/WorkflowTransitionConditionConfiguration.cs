using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class WorkflowTransitionConditionConfiguration : IEntityTypeConfiguration<WorkflowTransitionCondition>
{
    public void Configure(EntityTypeBuilder<WorkflowTransitionCondition> builder)
    {
        builder.ToTable("WorkflowTransitionConditions");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Subject).HasConversion<int>().IsRequired();
        builder.Property(entity => entity.Field).HasConversion<int>().IsRequired();
        builder.Property(entity => entity.Operator).HasConversion<int>().IsRequired();
        builder.Property(entity => entity.ExpectedValue).HasMaxLength(500);
        builder.Property(entity => entity.ExpectedValueSource).HasMaxLength(100);
        builder.Property(entity => entity.FailureMessageResourceKey).HasMaxLength(250);
        builder.Property(entity => entity.Order).IsRequired();
        builder.Property(entity => entity.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasOne(entity => entity.TransactionStatusTransition)
            .WithMany(transition => transition.Conditions)
            .HasForeignKey(entity => entity.TransactionStatusTransitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(entity => new { entity.TransactionStatusTransitionId, entity.Order })
            .HasFilter("\"DeletedDate\" IS NULL");

        builder.HasQueryFilter(entity =>
            entity.DeletedDate == null &&
            entity.TransactionStatusTransition.DeletedDate == null);
    }
}

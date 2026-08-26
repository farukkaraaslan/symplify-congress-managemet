using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class WorkflowTransitionEffectConfiguration : IEntityTypeConfiguration<WorkflowTransitionEffect>
{
    public void Configure(EntityTypeBuilder<WorkflowTransitionEffect> builder)
    {
        builder.ToTable("WorkflowTransitionEffects");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.EffectType).HasConversion<int>().IsRequired();
        builder.Property(entity => entity.ParametersJson).HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(entity => entity.Order).IsRequired();
        builder.Property(entity => entity.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasOne(entity => entity.TransactionStatusTransition)
            .WithMany(transition => transition.Effects)
            .HasForeignKey(entity => entity.TransactionStatusTransitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(entity => new { entity.TransactionStatusTransitionId, entity.Order })
            .HasFilter("\"DeletedDate\" IS NULL");

        builder.HasQueryFilter(entity =>
            entity.DeletedDate == null &&
            entity.TransactionStatusTransition.DeletedDate == null);
    }
}

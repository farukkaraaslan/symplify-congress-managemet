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

        builder.Property(entity => entity.Id)
            .HasColumnName("Id")
            .IsRequired();

        builder.Property(entity => entity.Code)
            .HasColumnName("Code")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(entity => entity.Order)
            .HasColumnName("Order")
            .IsRequired();

        builder.Property(entity => entity.IsActive)
            .HasColumnName("IsActive")
            .IsRequired();

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

        builder.HasIndex(entity => entity.Code)
            .IsUnique();

        builder.HasIndex(entity => entity.Order);
    }
}
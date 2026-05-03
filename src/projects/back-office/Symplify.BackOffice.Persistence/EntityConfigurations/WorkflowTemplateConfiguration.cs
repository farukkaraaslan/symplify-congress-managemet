using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Workflow.WorkflowTemplate;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class WorkflowTemplateConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("WorkflowTemplates");

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

        builder.Property(entity => entity.Code)
            .HasColumnName("Code")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(entity => entity.InitialTransactionStatusId)
            .HasColumnName("InitialTransactionStatusId")
            .IsRequired(false);

        builder.Property(entity => entity.IsDefault)
            .HasColumnName("IsDefault")
            .IsRequired()
            .HasDefaultValue(false);

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
        builder.HasOne(entity => entity.InitialTransactionStatus)
            .WithMany()
            .HasForeignKey(entity => entity.InitialTransactionStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(entity => entity.Translations)
            .WithOne(translation => translation.WorkflowTemplate)
            .HasForeignKey(translation => translation.WorkflowTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(entity => entity.Transitions)
            .WithOne(transition => transition.WorkflowTemplate)
            .HasForeignKey(transition => transition.WorkflowTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureIndexes(EntityTypeBuilder<EntityType> builder)
    {
        builder.HasIndex(entity => entity.Code)
            .IsUnique()
            .HasFilter("\"DeletedDate\" IS NULL");

        builder.HasIndex(entity => entity.IsDefault)
            .IsUnique()
            .HasFilter("\"IsDefault\" = TRUE AND \"DeletedDate\" IS NULL");
    }

    private static void ConfigureQueryFilters(EntityTypeBuilder<EntityType> builder)
    {
        builder.HasQueryFilter(entity => entity.DeletedDate == null);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Congress.CongressWorkflowSetting;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class CongressWorkflowSettingConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("CongressWorkflowSettings");

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

        builder.Property(entity => entity.CongressId)
            .HasColumnName("CongressId")
            .IsRequired();

        builder.Property(entity => entity.SourceWorkflowTemplateId)
            .HasColumnName("SourceWorkflowTemplateId")
            .IsRequired(false);

        builder.Property(entity => entity.InitialTransactionStatusId)
            .HasColumnName("InitialTransactionStatusId")
            .IsRequired(false);

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
        builder.HasOne(entity => entity.Congress)
            .WithOne(congress => congress.WorkflowSetting)
            .HasForeignKey<EntityType>(entity => entity.CongressId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entity => entity.SourceWorkflowTemplate)
            .WithMany(template => template.CongressWorkflowSettings)
            .HasForeignKey(entity => entity.SourceWorkflowTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.InitialTransactionStatus)
            .WithMany()
            .HasForeignKey(entity => entity.InitialTransactionStatusId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureIndexes(EntityTypeBuilder<EntityType> builder)
    {
        builder.HasIndex(entity => entity.CongressId)
            .IsUnique()
            .HasFilter("\"DeletedDate\" IS NULL");
    }

    private static void ConfigureQueryFilters(EntityTypeBuilder<EntityType> builder)
    {
        builder.HasQueryFilter(entity => entity.DeletedDate == null);
    }
}

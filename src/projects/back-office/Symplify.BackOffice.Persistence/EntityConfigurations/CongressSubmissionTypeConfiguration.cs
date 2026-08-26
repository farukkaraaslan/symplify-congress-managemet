using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Congress.CongressSubmissionType;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class CongressSubmissionTypeConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("CongressSubmissionTypes");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("Id")
            .IsRequired();

        builder.Property(entity => entity.CongressId)
            .HasColumnName("CongressId")
            .IsRequired();

        builder.Property(entity => entity.SubmissionTypeId)
            .HasColumnName("SubmissionTypeId")
            .IsRequired();

        builder.Property(entity => entity.Order)
            .HasColumnName("Order")
            .IsRequired();

        builder.Property(entity => entity.IsActive)
            .HasColumnName("IsActive")
            .IsRequired();

        builder.HasIndex(entity => new { entity.CongressId, entity.SubmissionTypeId })
            .IsUnique()
            .HasFilter("\"DeletedDate\" IS NULL")
            .HasDatabaseName("IX_CongressSubmissionTypes_CongressId_SubmissionTypeId");

        builder.HasIndex(entity => new { entity.CongressId, entity.Order })
            .HasDatabaseName("IX_CongressSubmissionTypes_CongressId_Order");

        builder.HasOne(entity => entity.Congress)
            .WithMany(congress => congress.SubmissionTypes)
            .HasForeignKey(entity => entity.CongressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.SubmissionType)
            .WithMany(submissionType => submissionType.CongressSubmissionTypes)
            .HasForeignKey(entity => entity.SubmissionTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

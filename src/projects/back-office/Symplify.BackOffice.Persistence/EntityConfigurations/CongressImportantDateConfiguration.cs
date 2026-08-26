using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Congress.CongressImportantDate;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class CongressImportantDateConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("CongressImportantDates");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("Id")
            .IsRequired();

        builder.Property(entity => entity.CongressId)
            .IsRequired();

        builder.Property(entity => entity.StartDate)
            .IsRequired();

        builder.Property(entity => entity.EndDate)
            .IsRequired();

        builder.Property(entity => entity.Order)
            .IsRequired();

        builder.Property(entity => entity.IsActive)
            .IsRequired();

        builder.HasOne(entity => entity.Congress)
            .WithMany(congress => congress.ImportantDates)
            .HasForeignKey(entity => entity.CongressId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(entity => entity.CongressId);

        builder.HasIndex(entity => new { entity.CongressId, entity.Order });
    }
}

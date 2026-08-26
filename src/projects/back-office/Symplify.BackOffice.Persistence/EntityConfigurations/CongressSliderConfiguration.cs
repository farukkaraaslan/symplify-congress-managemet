using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Congress.CongressSlider;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class CongressSliderConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("CongressSliders");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("Id")
            .IsRequired();

        builder.Property(entity => entity.CongressId)
            .IsRequired();

        builder.Property(entity => entity.ImagePath)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(entity => entity.Order)
            .HasDefaultValue(0)
            .IsRequired();

        builder.HasIndex(entity => entity.CongressId);
        builder.HasIndex(entity => new { entity.CongressId, entity.Order });
        builder.HasIndex(entity => new { entity.CongressId, entity.IsActive });

        builder.HasOne(entity => entity.Congress)
            .WithMany(congress => congress.Sliders)
            .HasForeignKey(entity => entity.CongressId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(entity => entity.Translations)
            .WithOne(translation => translation.CongressSlider)
            .HasForeignKey(translation => translation.CongressSliderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

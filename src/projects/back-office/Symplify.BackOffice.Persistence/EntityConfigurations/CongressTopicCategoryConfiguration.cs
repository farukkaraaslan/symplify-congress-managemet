using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Congress.CongressTopicCategory;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class CongressTopicCategoryConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("CongressTopicCategories");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.CongressId).IsRequired();
        builder.Property(entity => entity.Order).IsRequired();
        builder.Property(entity => entity.IsActive).IsRequired();

        builder.HasIndex(entity => new { entity.CongressId, entity.Order })
            .HasDatabaseName("IX_CongressTopicCategories_CongressId_Order");

        builder.HasOne(entity => entity.Congress)
            .WithMany(congress => congress.TopicCategories)
            .HasForeignKey(entity => entity.CongressId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

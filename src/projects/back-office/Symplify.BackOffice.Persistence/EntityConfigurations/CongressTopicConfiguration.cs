using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Congress.CongressTopic;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class CongressTopicConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("CongressTopics");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("Id")
            .IsRequired();

        builder.Property(entity => entity.CongressId)
            .HasColumnName("CongressId")
            .IsRequired();

        builder.Property(entity => entity.TopicId)
            .HasColumnName("TopicId")
            .IsRequired();

        builder.Property(entity => entity.CategoryId)
            .HasColumnName("CategoryId")
            .IsRequired(false);

        builder.Property(entity => entity.Order)
            .HasColumnName("Order")
            .IsRequired();

        builder.Property(entity => entity.IsActive)
            .HasColumnName("IsActive")
            .IsRequired();

        builder.HasIndex(entity => new { entity.CongressId, entity.TopicId })
            .IsUnique()
            .HasFilter("\"DeletedDate\" IS NULL")
            .HasDatabaseName("IX_CongressTopics_CongressId_TopicId");

        builder.HasIndex(entity => new { entity.CongressId, entity.Order })
            .HasDatabaseName("IX_CongressTopics_CongressId_Order");

        builder.HasIndex(entity => new { entity.CongressId, entity.CategoryId, entity.Order })
            .HasDatabaseName("IX_CongressTopics_CongressId_CategoryId_Order");

        builder.HasOne(entity => entity.Congress)
            .WithMany(congress => congress.Topics)
            .HasForeignKey(entity => entity.CongressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.Topic)
            .WithMany(topic => topic.CongressTopics)
            .HasForeignKey(entity => entity.TopicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.Category)
            .WithMany(category => category.Topics)
            .HasForeignKey(entity => entity.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

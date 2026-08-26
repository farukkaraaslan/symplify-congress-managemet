using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Lookups.EvaluationScoreOption;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class EvaluationScoreOptionConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("EvaluationScoreOptions");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasColumnName("Id").IsRequired();
        builder.Property(entity => entity.Value).HasColumnName("Value").HasColumnType("numeric(8,2)").IsRequired();
        builder.Property(entity => entity.Label).HasColumnName("Label").HasMaxLength(100);
        builder.Property(entity => entity.Order).HasColumnName("Order").IsRequired();
        builder.Property(entity => entity.IsActive).HasColumnName("IsActive").IsRequired();

        builder.HasIndex(entity => entity.Value).IsUnique();
        builder.HasIndex(entity => new { entity.IsActive, entity.Order });
    }
}

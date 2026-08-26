using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Congress.CongressPaymentPlan;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class CongressPaymentPlanConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("CongressPaymentPlans");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("Id")
            .IsRequired();

        builder.Property(entity => entity.CongressId)
            .HasColumnName("CongressId")
            .IsRequired();

        builder.Property(entity => entity.Code)
            .HasColumnName("Code")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(entity => entity.Amount)
            .HasColumnName("Amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(entity => entity.Currency)
            .HasColumnName("Currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(entity => entity.AudienceType)
            .HasColumnName("AudienceType")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(entity => entity.PaymentCategory)
            .HasColumnName("PaymentCategory")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(entity => entity.DueDate)
            .HasColumnName("DueDate");

        builder.Property(entity => entity.ValidFrom)
            .HasColumnName("ValidFrom");

        builder.Property(entity => entity.ValidUntil)
            .HasColumnName("ValidUntil");

        builder.Property(entity => entity.Order)
            .HasColumnName("Order")
            .IsRequired();

        builder.Property(entity => entity.IsPublicVisible)
            .HasColumnName("IsPublicVisible")
            .IsRequired();

        builder.Property(entity => entity.IsActive)
            .HasColumnName("IsActive")
            .IsRequired();

        builder.HasIndex(entity => new { entity.CongressId, entity.Code })
            .IsUnique()
            .HasFilter("\"DeletedDate\" IS NULL")
            .HasDatabaseName("IX_CongressPaymentPlans_CongressId_Code");

        builder.HasIndex(entity => new { entity.CongressId, entity.AudienceType, entity.IsActive, entity.IsPublicVisible })
            .HasDatabaseName("IX_CongressPaymentPlans_PublicLookup");

        builder.HasOne(entity => entity.Congress)
            .WithMany(congress => congress.PaymentPlans)
            .HasForeignKey(entity => entity.CongressId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

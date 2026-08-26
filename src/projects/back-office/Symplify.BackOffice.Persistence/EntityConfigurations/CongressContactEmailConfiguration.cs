using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class CongressContactEmailConfiguration : IEntityTypeConfiguration<CongressContactEmail>
{
    public void Configure(EntityTypeBuilder<CongressContactEmail> builder)
    {
        builder.ToTable("CongressContactEmails");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.CongressId)
            .IsRequired();

        builder.Property(entity => entity.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(entity => entity.Label)
            .HasMaxLength(100);

        builder.Property(entity => entity.IsPrimary)
            .IsRequired();

        builder.Property(entity => entity.IsVisibleOnPortal)
            .IsRequired();

        builder.Property(entity => entity.ReceivesContactMessages)
            .IsRequired();

        builder.Property(entity => entity.Order)
            .IsRequired();

        builder.HasIndex(entity => new { entity.CongressId, entity.Email })
            .HasDatabaseName("UX_CongressContactEmails_Congress_Email_Active")
            .IsUnique()
            .HasFilter("\"DeletedDate\" IS NULL");

        builder.HasIndex(entity => entity.CongressId)
            .HasDatabaseName("UX_CongressContactEmails_Congress_Primary_Active")
            .IsUnique()
            .HasFilter("\"IsPrimary\" = TRUE AND \"DeletedDate\" IS NULL");

        builder.HasIndex(entity => new { entity.CongressId, entity.Order })
            .HasDatabaseName("IX_CongressContactEmails_Congress_Order");

        builder.HasOne(entity => entity.Congress)
            .WithMany(congress => congress.ContactEmails)
            .HasForeignKey(entity => entity.CongressId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

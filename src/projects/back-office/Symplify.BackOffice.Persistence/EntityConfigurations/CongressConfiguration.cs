using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Congress.Congress;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class CongressConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("Congresses");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("Id")
            .IsRequired();

        builder.Property(entity => entity.OrganizationId)
            .IsRequired();

        builder.Property(entity => entity.Code)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(entity => entity.Name)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(entity => entity.Slug)
            .HasMaxLength(200);

        builder.Property(entity => entity.EditionNumber);

        builder.Property(entity => entity.StartDate);

        builder.Property(entity => entity.EndDate);

        builder.Property(entity => entity.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(entity => entity.ContactName)
            .HasMaxLength(200);

        builder.Property(entity => entity.ContactTitle)
            .HasMaxLength(200);

        builder.Property(entity => entity.ContactEmail)
            .HasMaxLength(256);

        builder.Property(entity => entity.ContactPhone)
            .HasMaxLength(50);

        builder.Property(entity => entity.ContactAddress)
            .HasMaxLength(1000);

        builder.Property(entity => entity.VenueName)
            .HasMaxLength(300);

        builder.Property(entity => entity.LogoLightPath)
            .HasMaxLength(500);

        builder.Property(entity => entity.LogoDarkPath)
            .HasMaxLength(500);

        builder.HasIndex(entity => new { entity.OrganizationId, entity.Code })
            .IsUnique();

        builder.HasIndex(entity => new { entity.OrganizationId, entity.Slug })
            .IsUnique()
            .HasFilter("\"Slug\" IS NOT NULL");

        builder.HasIndex(entity => entity.Status);
        builder.HasIndex(entity => entity.StartDate);
        builder.HasIndex(entity => entity.EndDate);

        builder.HasOne(entity => entity.Organization)
            .WithMany(organization => organization.Congresses)
            .HasForeignKey(entity => entity.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.Country)
            .WithMany()
            .HasForeignKey(entity => entity.CountryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(entity => entity.City)
            .WithMany()
            .HasForeignKey(entity => entity.CityId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(entity => entity.State)
            .WithMany()
            .HasForeignKey(entity => entity.StateId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(entity => entity.Translations)
            .WithOne(translation => translation.Congress)
            .HasForeignKey(translation => translation.CongressId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(entity => entity.ContactEmails)
            .WithOne(contactEmail => contactEmail.Congress)
            .HasForeignKey(contactEmail => contactEmail.CongressId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(entity => entity.Announcements)
            .WithOne(announcement => announcement.Congress)
            .HasForeignKey(announcement => announcement.CongressId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

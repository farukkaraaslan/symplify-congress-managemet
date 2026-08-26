using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Symplify.BackOffice.Domain.Enums;
using EntityType = Symplify.BackOffice.Domain.Congress.CongressAnnouncement;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class CongressAnnouncementConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("CongressAnnouncements");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("Id")
            .IsRequired();

        builder.Property(entity => entity.CongressId)
            .IsRequired();

        builder.Property(entity => entity.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(entity => entity.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(entity => entity.PublishStartDate);

        builder.Property(entity => entity.PublishEndDate);

        builder.Property(entity => entity.IsPinned)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(entity => entity.ShowOnHomePage)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(entity => entity.ShowInTicker)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(entity => entity.ExternalUrl)
            .HasMaxLength(1000);

        builder.Property(entity => entity.AttachmentPath)
            .HasMaxLength(500);

        builder.Property(entity => entity.Order)
            .HasDefaultValue(0)
            .IsRequired();

        builder.HasIndex(entity => entity.CongressId);
        builder.HasIndex(entity => new { entity.CongressId, entity.Status });
        builder.HasIndex(entity => new { entity.CongressId, entity.IsActive });
        builder.HasIndex(entity => new { entity.CongressId, entity.ShowOnHomePage });
        builder.HasIndex(entity => new { entity.CongressId, entity.ShowInTicker });
        builder.HasIndex(entity => new { entity.CongressId, entity.IsPinned, entity.Order });
        builder.HasIndex(entity => entity.PublishStartDate);
        builder.HasIndex(entity => entity.PublishEndDate);

        builder.HasOne(entity => entity.Congress)
            .WithMany(congress => congress.Announcements)
            .HasForeignKey(entity => entity.CongressId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(entity => entity.Translations)
            .WithOne(translation => translation.CongressAnnouncement)
            .HasForeignKey(translation => translation.CongressAnnouncementId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

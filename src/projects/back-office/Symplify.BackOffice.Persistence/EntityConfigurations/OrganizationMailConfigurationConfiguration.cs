using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Symplify.BackOffice.Domain.Organization;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class OrganizationMailConfigurationConfiguration : IEntityTypeConfiguration<OrganizationMailConfiguration>
{
    public void Configure(EntityTypeBuilder<OrganizationMailConfiguration> builder)
    {
        builder.ToTable("OrganizationMailConfigurations", table =>
            table.HasCheckConstraint("CK_OrganizationMailConfigurations_Port", "\"Port\" BETWEEN 1 AND 65535"));

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.OrganizationId).IsRequired();
        builder.Property(entity => entity.Host).HasMaxLength(250).IsRequired();
        builder.Property(entity => entity.Port).IsRequired().HasDefaultValue(587);
        builder.Property(entity => entity.EnableSsl).IsRequired().HasDefaultValue(true);
        builder.Property(entity => entity.Username).HasMaxLength(250).IsRequired();
        builder.Property(entity => entity.PasswordCipherText).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.FromEmail).HasMaxLength(250).IsRequired();
        builder.Property(entity => entity.FromName).HasMaxLength(250).IsRequired();
        builder.Property(entity => entity.ReplyToEmail).HasMaxLength(250);
        builder.Property(entity => entity.ReplyToName).HasMaxLength(250);
        builder.Property(entity => entity.MailLogoBucketName).HasMaxLength(150);
        builder.Property(entity => entity.MailLogoObjectName).HasMaxLength(1000);
        builder.Property(entity => entity.MailLogoContentType).HasMaxLength(100);
        builder.Property(entity => entity.MailLogoFileName).HasMaxLength(255);
        builder.Property(entity => entity.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(entity => entity.LastTestError).HasMaxLength(1000);
        builder.Property(entity => entity.CreatedBy).HasMaxLength(100);
        builder.Property(entity => entity.UpdatedBy).HasMaxLength(100);
        builder.Property(entity => entity.DeletedBy).HasMaxLength(100);

        builder.HasOne(entity => entity.Organization)
            .WithOne(organization => organization.MailConfiguration)
            .HasForeignKey<OrganizationMailConfiguration>(entity => entity.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(entity => entity.OrganizationId)
            .IsUnique()
            .HasFilter("\"DeletedDate\" IS NULL");

        builder.HasQueryFilter(entity => entity.DeletedDate == null);
    }
}

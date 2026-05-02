using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Symplify.BackOffice.Domain.Identity;

namespace Symplify.BackOffice.Persistence.EntityConfigurations.Identity;

public sealed class AppRoleConfiguration : IEntityTypeConfiguration<AppRole>
{
    public void Configure(EntityTypeBuilder<AppRole> builder)
    {
        builder.ToTable("Roles");

        builder.Property(role => role.Id)
            .HasColumnName("Id")
            .HasColumnOrder(0)
            .IsRequired();

        builder.Property(role => role.Name)
            .HasColumnName("Name")
            .HasColumnOrder(1)
            .HasMaxLength(256);

        builder.Property(role => role.NormalizedName)
            .HasColumnName("NormalizedName")
            .HasColumnOrder(2)
            .HasMaxLength(256);

        builder.Property(role => role.Description)
            .HasColumnName("Description")
            .HasColumnOrder(3)
            .HasMaxLength(500);

        builder.Property(role => role.ConcurrencyStamp)
            .HasColumnName("ConcurrencyStamp")
            .HasColumnOrder(20)
            .IsConcurrencyToken();

        builder.Property(role => role.CreatedBy)
            .HasColumnName("CreatedBy")
            .HasColumnOrder(90)
            .HasMaxLength(100);

        builder.Property(role => role.UpdatedBy)
            .HasColumnName("UpdatedBy")
            .HasColumnOrder(91)
            .HasMaxLength(100);

        builder.Property(role => role.DeletedBy)
            .HasColumnName("DeletedBy")
            .HasColumnOrder(92)
            .HasMaxLength(100);

        builder.Property(role => role.CreatedDate)
            .HasColumnName("CreatedDate")
            .HasColumnOrder(93)
            .IsRequired();

        builder.Property(role => role.UpdatedDate)
            .HasColumnName("UpdatedDate")
            .HasColumnOrder(94);

        builder.Property(role => role.DeletedDate)
            .HasColumnName("DeletedDate")
            .HasColumnOrder(95);

        builder.HasIndex(role => role.NormalizedName)
            .HasDatabaseName("RoleNameIndex")
            .IsUnique();
    }
}
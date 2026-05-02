using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Symplify.BackOffice.Domain.Identity;

namespace Symplify.BackOffice.Persistence.EntityConfigurations.Identity;

public sealed class AppRoleClaimConfiguration : IEntityTypeConfiguration<AppRoleClaim>
{
    public void Configure(EntityTypeBuilder<AppRoleClaim> builder)
    {
        builder.ToTable("RoleClaims");

        builder.HasKey(roleClaim => roleClaim.Id);

        builder.Property(roleClaim => roleClaim.Id)
            .HasColumnName("Id")
            .HasColumnOrder(0)
            .IsRequired();

        builder.Property(roleClaim => roleClaim.RoleId)
            .HasColumnName("RoleId")
            .HasColumnOrder(1)
            .IsRequired();

        builder.Property(roleClaim => roleClaim.ClaimType)
            .HasColumnName("ClaimType")
            .HasColumnOrder(2)
            .HasMaxLength(256);

        builder.Property(roleClaim => roleClaim.ClaimValue)
            .HasColumnName("ClaimValue")
            .HasColumnOrder(3)
            .HasMaxLength(512);

        builder.Property(roleClaim => roleClaim.DisplayName)
            .HasColumnName("DisplayName")
            .HasColumnOrder(4)
            .HasMaxLength(200);

        builder.Property(roleClaim => roleClaim.Description)
           .HasColumnName("Description")
           .HasColumnOrder(5)
           .HasMaxLength(200);

        builder.Property(roleClaim => roleClaim.Module)
            .HasColumnName("Module")
            .HasColumnOrder(6)
            .HasMaxLength(100);

        builder.Property(roleClaim => roleClaim.CreatedBy)
            .HasColumnName("CreatedBy")
            .HasColumnOrder(90)
            .HasMaxLength(100);

        builder.Property(roleClaim => roleClaim.UpdatedBy)
            .HasColumnName("UpdatedBy")
            .HasColumnOrder(91)
            .HasMaxLength(100);

        builder.Property(roleClaim => roleClaim.DeletedBy)
            .HasColumnName("DeletedBy")
            .HasColumnOrder(92)
            .HasMaxLength(100);

        builder.Property(roleClaim => roleClaim.CreatedDate)
            .HasColumnName("CreatedDate")
            .HasColumnOrder(93)
            .IsRequired();

        builder.Property(roleClaim => roleClaim.UpdatedDate)
            .HasColumnName("UpdatedDate")
            .HasColumnOrder(94);

        builder.Property(roleClaim => roleClaim.DeletedDate)
            .HasColumnName("DeletedDate")
            .HasColumnOrder(95);

        builder.HasIndex(roleClaim => roleClaim.RoleId)
            .HasDatabaseName("IX_RoleClaims_RoleId");

        builder.HasOne<AppRole>()
            .WithMany()
            .HasForeignKey(roleClaim => roleClaim.RoleId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
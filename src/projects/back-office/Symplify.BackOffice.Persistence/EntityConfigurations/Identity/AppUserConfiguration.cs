using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Symplify.BackOffice.Domain.Identity;

namespace Symplify.BackOffice.Persistence.EntityConfigurations.Identity;

public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("Users");

        builder.Property(user => user.Id)
            .HasColumnName("Id")
            .HasColumnOrder(0)
            .IsRequired();

        builder.Property(user => user.UserName)
            .HasColumnName("UserName")
            .HasColumnOrder(1)
            .HasMaxLength(256);

        builder.Property(user => user.NormalizedUserName)
            .HasColumnName("NormalizedUserName")
            .HasColumnOrder(2)
            .HasMaxLength(256);

        builder.Property(user => user.Email)
            .HasColumnName("Email")
            .HasColumnOrder(3)
            .HasMaxLength(256);

        builder.Property(user => user.NormalizedEmail)
            .HasColumnName("NormalizedEmail")
            .HasColumnOrder(4)
            .HasMaxLength(256);

        builder.Property(user => user.EmailConfirmed)
            .HasColumnName("EmailConfirmed")
            .HasColumnOrder(5)
            .IsRequired();

        builder.Property(user => user.Name)
            .HasColumnName("Name")
            .HasColumnOrder(6)
            .HasMaxLength(100);

        builder.Property(user => user.Surname)
            .HasColumnName("Surname")
            .HasColumnOrder(7)
            .HasMaxLength(100);

        builder.Property(user => user.TitleId)
            .HasColumnName("TitleId")
            .HasColumnOrder(8);

        builder.Property(user => user.Institution)
         .HasColumnName("Institution")
         .HasColumnOrder(9);

        builder.Property(user => user.Orcid)
         .HasColumnName("Orcid")
         .HasColumnOrder(10);

        builder.Property(user => user.IsBlacklisted)
            .HasColumnName("IsBlacklisted")
            .HasColumnOrder(11)
            .IsRequired();

        builder.Property(user => user.PhoneNumber)
            .HasColumnName("PhoneNumber")
            .HasColumnOrder(12)
            .HasMaxLength(50);

        builder.Property(user => user.PhoneNumberConfirmed)
            .HasColumnName("PhoneNumberConfirmed")
            .HasColumnOrder(13)
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasColumnName("PasswordHash")
            .HasColumnOrder(20);

        builder.Property(user => user.SecurityStamp)
            .HasColumnName("SecurityStamp")
            .HasColumnOrder(21);

        builder.Property(user => user.ConcurrencyStamp)
            .HasColumnName("ConcurrencyStamp")
            .HasColumnOrder(22)
            .IsConcurrencyToken();

        builder.Property(user => user.TwoFactorEnabled)
            .HasColumnName("TwoFactorEnabled")
            .HasColumnOrder(23)
            .IsRequired();

        builder.Property(user => user.LockoutEnd)
            .HasColumnName("LockoutEnd")
            .HasColumnOrder(24);

        builder.Property(user => user.LockoutEnabled)
            .HasColumnName("LockoutEnabled")
            .HasColumnOrder(25)
            .IsRequired();

        builder.Property(user => user.AccessFailedCount)
            .HasColumnName("AccessFailedCount")
            .HasColumnOrder(26)
            .IsRequired();

        builder.Property(user => user.CreatedBy)
            .HasColumnName("CreatedBy")
            .HasColumnOrder(90)
            .HasMaxLength(100);

        builder.Property(user => user.UpdatedBy)
            .HasColumnName("UpdatedBy")
            .HasColumnOrder(91)
            .HasMaxLength(100);

        builder.Property(user => user.DeletedBy)
            .HasColumnName("DeletedBy")
            .HasColumnOrder(92)
            .HasMaxLength(100);

        builder.Property(user => user.CreatedDate)
            .HasColumnName("CreatedDate")
            .HasColumnOrder(93)
            .IsRequired();

        builder.Property(user => user.UpdatedDate)
            .HasColumnName("UpdatedDate")
            .HasColumnOrder(94);

        builder.Property(user => user.DeletedDate)
            .HasColumnName("DeletedDate")
            .HasColumnOrder(95);

        builder.HasIndex(user => user.NormalizedUserName)
            .HasDatabaseName("UserNameIndex")
            .IsUnique();

        builder.HasIndex(user => user.NormalizedEmail)
            .HasDatabaseName("EmailIndex");
    }
}
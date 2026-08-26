using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Symplify.BackOffice.Domain.Identity;

namespace Symplify.BackOffice.Persistence.EntityConfigurations.Identity;

public sealed class IdentityUserRoleConfiguration : IEntityTypeConfiguration<IdentityUserRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole<Guid>> builder)
    {
        builder.ToTable("UserRoles");

        builder.HasKey(userRole => new
        {
            userRole.UserId,
            userRole.RoleId
        });

        builder.Property(userRole => userRole.UserId)
            .HasColumnName("UserId")
            .IsRequired();

        builder.Property(userRole => userRole.RoleId)
            .HasColumnName("RoleId")
            .IsRequired();

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(userRole => userRole.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppRole>()
            .WithMany()
            .HasForeignKey(userRole => userRole.RoleId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}

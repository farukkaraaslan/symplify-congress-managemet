using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Symplify.BackOffice.Persistence.EntityConfigurations.Identity;

public sealed class IdentityUserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> builder)
    {
        builder.ToTable("UserLogins");

        builder.HasKey(userLogin => new
        {
            userLogin.LoginProvider,
            userLogin.ProviderKey
        });

        builder.Property(userLogin => userLogin.LoginProvider)
            .HasColumnName("LoginProvider")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(userLogin => userLogin.ProviderKey)
            .HasColumnName("ProviderKey")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(userLogin => userLogin.ProviderDisplayName)
            .HasColumnName("ProviderDisplayName");

        builder.Property(userLogin => userLogin.UserId)
            .HasColumnName("UserId")
            .IsRequired();

        builder.HasIndex(userLogin => userLogin.UserId);
    }
}

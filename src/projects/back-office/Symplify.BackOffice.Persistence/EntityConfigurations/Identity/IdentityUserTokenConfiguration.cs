using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Symplify.BackOffice.Persistence.EntityConfigurations.Identity;

public sealed class IdentityUserTokenConfiguration : IEntityTypeConfiguration<IdentityUserToken<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserToken<Guid>> builder)
    {
        builder.ToTable("UserTokens");

        builder.HasKey(userToken => new
        {
            userToken.UserId,
            userToken.LoginProvider,
            userToken.Name
        });

        builder.Property(userToken => userToken.UserId)
            .HasColumnName("UserId")
            .IsRequired();

        builder.Property(userToken => userToken.LoginProvider)
            .HasColumnName("LoginProvider")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(userToken => userToken.Name)
            .HasColumnName("Name")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(userToken => userToken.Value)
            .HasColumnName("Value");
    }
}

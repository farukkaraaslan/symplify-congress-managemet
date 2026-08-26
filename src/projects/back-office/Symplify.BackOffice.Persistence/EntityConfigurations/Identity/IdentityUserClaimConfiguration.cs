using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Symplify.BackOffice.Persistence.EntityConfigurations.Identity;

public sealed class IdentityUserClaimConfiguration : IEntityTypeConfiguration<IdentityUserClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> builder)
    {
        builder.ToTable("UserClaims");

        builder.HasKey(userClaim => userClaim.Id);

        builder.Property(userClaim => userClaim.Id)
            .HasColumnName("Id")
            .IsRequired();

        builder.Property(userClaim => userClaim.UserId)
            .HasColumnName("UserId")
            .IsRequired();

        builder.Property(userClaim => userClaim.ClaimType)
            .HasColumnName("ClaimType");

        builder.Property(userClaim => userClaim.ClaimValue)
            .HasColumnName("ClaimValue");

        builder.HasIndex(userClaim => userClaim.UserId);
    }
}

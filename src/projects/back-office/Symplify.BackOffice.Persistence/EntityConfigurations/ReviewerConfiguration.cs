using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Submission.Reviewer;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class ReviewerConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("Reviewers");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("Id")
            .IsRequired();

        builder.Property(entity => entity.UserId)
            .IsRequired();

        builder.Property(entity => entity.Status)
            .IsRequired();

        builder.Property(entity => entity.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(entity => entity.UserId)
            .IsUnique()
            .HasFilter("\"DeletedDate\" IS NULL");

        builder.HasIndex(entity => entity.IsActive);

        builder.HasOne(entity => entity.User)
            .WithMany(user => user.Reviewers)
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

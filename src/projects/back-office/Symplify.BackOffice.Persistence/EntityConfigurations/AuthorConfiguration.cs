using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Submission.Author;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class AuthorConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("Authors");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasColumnName("Id").IsRequired();
        builder.Property(entity => entity.TitleId).HasColumnName("TitleId");

        builder.HasOne(entity => entity.Title)
            .WithMany()
            .HasForeignKey(entity => entity.TitleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

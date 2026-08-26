using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Congress.CongressProgramItem;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class CongressProgramItemConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("CongressProgramItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Order).IsRequired();
        builder.Property(x => x.DurationMinutes).IsRequired();
        builder.Property(x => x.IsLocked).IsRequired();
        builder.Property(x => x.Source).HasConversion<int>().IsRequired();

        builder.HasOne(x => x.ProgramSession)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.ProgramSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Submission)
            .WithMany()
            .HasForeignKey(x => x.SubmissionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ProgramSessionId, x.Order });
        builder.HasIndex(x => x.SubmissionId)
            .IsUnique()
            .HasFilter("\"DeletedDate\" IS NULL");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Congress.CongressProgramPlan;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class CongressProgramPlanConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("CongressProgramPlans");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.VersionNo).IsRequired();
        builder.Property(x => x.DefaultPresentationDurationMinutes).IsRequired();
        builder.Property(x => x.DefaultSessionDurationMinutes).IsRequired();
        builder.Property(x => x.DefaultQuestionAnswerDurationMinutes).IsRequired();
        builder.Property(x => x.DefaultBreakDurationMinutes).IsRequired();
        builder.Property(x => x.SubmissionFilterJson).HasColumnType("text");
        builder.Property(x => x.EligibleSubmissionIdsJson).HasColumnType("text");

        builder.HasOne(x => x.Congress)
            .WithMany()
            .HasForeignKey(x => x.CongressId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.CongressId)
            .IsUnique()
            .HasFilter("\"DeletedDate\" IS NULL");
    }
}

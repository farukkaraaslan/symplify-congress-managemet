using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Congress.CongressProgramDay;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class CongressProgramDayConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("CongressProgramDays");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Date).HasColumnType("date").IsRequired();
        builder.Property(x => x.StartTime).HasColumnType("time without time zone").IsRequired();
        builder.Property(x => x.EndTime).HasColumnType("time without time zone").IsRequired();
        builder.Property(x => x.Order).IsRequired();

        builder.HasOne(x => x.ProgramPlan)
            .WithMany(x => x.Days)
            .HasForeignKey(x => x.ProgramPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ProgramPlanId, x.Date })
            .IsUnique()
            .HasFilter("\"DeletedDate\" IS NULL");
    }
}

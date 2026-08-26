using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Congress.CongressProgramFixedBlock;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class CongressProgramFixedBlockConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("CongressProgramFixedBlocks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BlockType).HasConversion<int>().IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.StartTime).HasColumnType("time without time zone").IsRequired();
        builder.Property(x => x.EndTime).HasColumnType("time without time zone").IsRequired();
        builder.Property(x => x.Order).IsRequired();
        builder.Property(x => x.IsLocked).IsRequired();

        builder.HasOne(x => x.ProgramDay)
            .WithMany(x => x.FixedBlocks)
            .HasForeignKey(x => x.ProgramDayId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.EventRoom)
            .WithMany()
            .HasForeignKey(x => x.EventRoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ProgramDayId, x.EventRoomId, x.StartTime });
    }
}

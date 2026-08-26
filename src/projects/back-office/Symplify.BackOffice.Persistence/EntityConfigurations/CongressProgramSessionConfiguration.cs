using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Congress.CongressProgramSession;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class CongressProgramSessionConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("CongressProgramSessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.StartTime).HasColumnType("time without time zone").IsRequired();
        builder.Property(x => x.EndTime).HasColumnType("time without time zone").IsRequired();
        builder.Property(x => x.QuestionAnswerDurationMinutes).IsRequired();
        builder.Property(x => x.Order).IsRequired();
        builder.Property(x => x.IsLocked).IsRequired();
        builder.Property(x => x.ChairAuthorId).IsRequired(false);
        builder.Property(x => x.ChairBoardMemberId).IsRequired(false);
        builder.Property(x => x.ViceChairAuthorId).IsRequired(false);
        builder.Property(x => x.ViceChairBoardMemberId).IsRequired(false);

        builder.HasOne(x => x.ProgramDay)
            .WithMany(x => x.Sessions)
            .HasForeignKey(x => x.ProgramDayId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.EventRoom)
            .WithMany()
            .HasForeignKey(x => x.EventRoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ChairAuthor)
            .WithMany()
            .HasForeignKey(x => x.ChairAuthorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.ChairBoardMember)
            .WithMany()
            .HasForeignKey(x => x.ChairBoardMemberId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.ViceChairAuthor)
            .WithMany()
            .HasForeignKey(x => x.ViceChairAuthorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.ViceChairBoardMember)
            .WithMany()
            .HasForeignKey(x => x.ViceChairBoardMemberId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.ProgramDayId, x.EventRoomId, x.StartTime });
        builder.HasIndex(x => x.ChairAuthorId);
        builder.HasIndex(x => x.ChairBoardMemberId);
        builder.HasIndex(x => x.ViceChairAuthorId);
        builder.HasIndex(x => x.ViceChairBoardMemberId);
    }
}

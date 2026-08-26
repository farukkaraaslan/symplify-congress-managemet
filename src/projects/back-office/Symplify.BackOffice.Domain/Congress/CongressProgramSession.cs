using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Lookups;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressProgramSession : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid ProgramDayId { get; set; }
    public Guid EventRoomId { get; set; }
    public string Title { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int QuestionAnswerDurationMinutes { get; set; }
    public int Order { get; set; }
    public bool IsLocked { get; set; }

    // Session officials can be selected from eligible congress authors or
    // active congress board members. Each role can reference only one source.
    public Guid? ChairAuthorId { get; set; }
    public Guid? ChairBoardMemberId { get; set; }
    public Guid? ViceChairAuthorId { get; set; }
    public Guid? ViceChairBoardMemberId { get; set; }

    public virtual CongressProgramDay ProgramDay { get; set; } = null!;
    public virtual EventRoom EventRoom { get; set; } = null!;
    public virtual Author? ChairAuthor { get; set; }
    public virtual CongressBoardMember? ChairBoardMember { get; set; }
    public virtual Author? ViceChairAuthor { get; set; }
    public virtual CongressBoardMember? ViceChairBoardMember { get; set; }
    public virtual ICollection<CongressProgramItem> Items { get; set; } = new HashSet<CongressProgramItem>();
}

using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Enums;
using SubmissionEntity = Symplify.BackOffice.Domain.Submission.Submission;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressProgramItem : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid ProgramSessionId { get; set; }
    public Guid SubmissionId { get; set; }
    public int Order { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsLocked { get; set; }
    public CongressProgramItemSource Source { get; set; } = CongressProgramItemSource.Automatic;

    public virtual CongressProgramSession ProgramSession { get; set; } = null!;
    public virtual SubmissionEntity Submission { get; set; } = null!;
}

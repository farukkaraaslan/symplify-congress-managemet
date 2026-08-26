using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressProgramDay : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid ProgramPlanId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int Order { get; set; }

    public virtual CongressProgramPlan ProgramPlan { get; set; } = null!;
    public virtual ICollection<CongressProgramSession> Sessions { get; set; } = new HashSet<CongressProgramSession>();
    public virtual ICollection<CongressProgramFixedBlock> FixedBlocks { get; set; } = new HashSet<CongressProgramFixedBlock>();
}

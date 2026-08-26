using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressProgramFixedBlock : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid ProgramDayId { get; set; }
    public Guid? EventRoomId { get; set; }
    public CongressProgramFixedBlockType BlockType { get; set; }
    public string Title { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int Order { get; set; }
    public bool IsLocked { get; set; } = true;

    public virtual CongressProgramDay ProgramDay { get; set; } = null!;
    public virtual EventRoom? EventRoom { get; set; }
}

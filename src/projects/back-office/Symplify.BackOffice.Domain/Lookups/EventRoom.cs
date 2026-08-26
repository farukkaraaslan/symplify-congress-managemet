using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Lookups;

public class EventRoom : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public string? Code { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<EventRoomTranslation> Translations { get; set; } = new HashSet<EventRoomTranslation>();
}

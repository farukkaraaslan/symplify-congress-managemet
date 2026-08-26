using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Localization;

namespace Symplify.BackOffice.Domain.Lookups;

public class EventRoomTranslation : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid EventRoomId { get; set; }
    public Guid LanguageId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public virtual EventRoom EventRoom { get; set; } = null!;
    public virtual Language Language { get; set; } = null!;
}

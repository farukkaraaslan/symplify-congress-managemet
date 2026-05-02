using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Localization;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressBoardTranslation : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressBoardId { get; set; }
    public Guid LanguageId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public virtual CongressBoard CongressBoard { get; set; } = null!;
    public virtual Language Language { get; set; } = null!;
}

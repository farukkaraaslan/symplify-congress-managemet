using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Localization;

namespace Symplify.BackOffice.Domain.Lookups;

public class TitleTranslation : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid TitleId { get; set; }
    public Guid LanguageId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public virtual Title Title { get; set; } = null!;
    public virtual Language Language { get; set; } = null!;
}

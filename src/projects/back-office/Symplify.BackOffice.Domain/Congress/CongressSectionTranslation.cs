using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Localization;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressSectionTranslation : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressSectionId { get; set; }
    public Guid LanguageId { get; set; }
    public string Title { get; set; } = null!;
    public string? Content { get; set; }

    public virtual CongressSection CongressSection { get; set; } = null!;
    public virtual Language Language { get; set; } = null!;
}

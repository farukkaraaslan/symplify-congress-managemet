using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Localization;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressDocumentTranslation : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressDocumentId { get; set; }

    public Guid LanguageId { get; set; }

    public string? Description { get; set; }

    public virtual CongressDocument CongressDocument { get; set; } = null!;

    public virtual Language Language { get; set; } = null!;
}

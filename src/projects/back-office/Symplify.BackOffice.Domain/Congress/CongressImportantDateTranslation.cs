using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Localization;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressImportantDateTranslation : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressImportantDateId { get; set; }
    public Guid LanguageId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }

    public virtual CongressImportantDate CongressImportantDate { get; set; } = null!;
    public virtual Language Language { get; set; } = null!;
}

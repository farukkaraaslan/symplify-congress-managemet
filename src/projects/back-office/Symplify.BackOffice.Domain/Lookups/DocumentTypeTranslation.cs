using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Localization;

namespace Symplify.BackOffice.Domain.Lookups;

public class DocumentTypeTranslation : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid DocumentTypeId { get; set; }
    public Guid LanguageId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public virtual DocumentType DocumentType { get; set; } = null!;
    public virtual Language Language { get; set; } = null!;
}

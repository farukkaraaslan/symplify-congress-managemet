using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressDocument : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressId { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public CongressDocumentType? Type { get; set; }
    public string FilePath { get; set; } = null!;
    public string? OriginalFileName { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Congress Congress { get; set; } = null!;
    public virtual DocumentType? DocumentType { get; set; }
}

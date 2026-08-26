using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressDocument : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressId { get; set; }

    public Guid? DocumentTypeId { get; set; }

    /// <summary>
    /// Legacy/display compatible path. For object storage this value mirrors ObjectName.
    /// </summary>
    public string FilePath { get; set; } = null!;

    /// <summary>
    /// User-uploaded original filename. Object storage filename is generated separately and stored in ObjectName.
    /// </summary>
    public string? OriginalFileName { get; set; }

    public string? StorageProvider { get; set; }

    public string? BucketName { get; set; }

    public string? ObjectName { get; set; }

    public string? ContentType { get; set; }

    public string? FileExtension { get; set; }

    public long? FileSize { get; set; }

    public string? ETag { get; set; }

    public string? CoverImagePath { get; set; }

    public string? CoverImageStorageProvider { get; set; }

    public string? CoverImageBucketName { get; set; }

    public string? CoverImageObjectName { get; set; }

    public string? CoverImageFileName { get; set; }

    public string? CoverImageContentType { get; set; }

    public long? CoverImageFileSize { get; set; }

    public string? CoverImageETag { get; set; }

    public int Order { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual Congress Congress { get; set; } = null!;

    public virtual DocumentType? DocumentType { get; set; }

    public virtual ICollection<CongressDocumentTranslation> Translations { get; set; } = new List<CongressDocumentTranslation>();
}

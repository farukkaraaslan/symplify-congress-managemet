using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressBoardMember : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressBoardId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? AcademicTitle { get; set; }

    public string? Institution { get; set; }

    /// <summary>
    /// Legacy/display compatible value. For object storage this value mirrors ImageObjectName.
    /// </summary>
    public string? ImagePath { get; set; }

    public string? ImageStorageProvider { get; set; }

    public string? ImageBucketName { get; set; }

    public string? ImageObjectName { get; set; }

    public string? ImageFileName { get; set; }

    public string? ImageContentType { get; set; }

    public long? ImageFileSize { get; set; }

    public string? ImageETag { get; set; }

    /// <summary>
    /// True ise bu kurul üyesi kabul mektubu üzerinde imzalayan kişi olarak kullanılır.
    /// Bir kongrede aynı anda yalnızca bir aktif imzacı kullanılmalıdır.
    /// </summary>
    public bool IsAcceptanceLetterSigner { get; set; }

    /// <summary>
    /// Legacy/display compatible value. For object storage this value mirrors SignatureObjectName.
    /// </summary>
    public string? SignaturePath { get; set; }

    public string? SignatureStorageProvider { get; set; }

    public string? SignatureBucketName { get; set; }

    public string? SignatureObjectName { get; set; }

    public string? SignatureFileName { get; set; }

    public string? SignatureContentType { get; set; }

    public long? SignatureFileSize { get; set; }

    public string? SignatureETag { get; set; }

    public int Order { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual CongressBoard CongressBoard { get; set; } = null!;

    public virtual ICollection<CongressBoardMemberTranslation> Translations { get; set; } = new HashSet<CongressBoardMemberTranslation>();
}

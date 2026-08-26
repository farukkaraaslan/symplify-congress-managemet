namespace Symplify.BackOffice.Application.Features.CongressDocuments.Queries.GetById;

public class GetByIdCongressDocumentResponse
{
    public Guid Id { get; set; }

    public Guid CongressId { get; set; }

    public Guid? DocumentTypeId { get; set; }

    public string FilePath { get; set; } = string.Empty;

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

    public bool IsActive { get; set; }
}

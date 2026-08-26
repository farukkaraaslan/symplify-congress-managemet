namespace Symplify.BackOffice.Application.Features.CongressDocuments.Queries.GetList;

public class GetListCongressDocumentListItemDto
{
    public Guid Id { get; set; }

    public Guid CongressId { get; set; }

    public Guid? DocumentTypeId { get; set; }

    public string? DocumentTypeName { get; set; }

    public string? Description { get; set; }

    public string? OriginalFileName { get; set; }

    public string? BucketName { get; set; }

    public string? ObjectName { get; set; }

    public string? ContentType { get; set; }

    public string? FileExtension { get; set; }

    public long? FileSize { get; set; }

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

    public Guid DisplayLanguageId { get; set; }

    public bool IsFallback { get; set; }
}

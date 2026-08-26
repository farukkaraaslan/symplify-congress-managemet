using Microsoft.AspNetCore.Http;

namespace Symplify.BackOffice.WebUI.Models.CongressDocuments;

public sealed class UpdateCongressDocumentViewModel
{
    public Guid Id { get; set; }

    public Guid CongressId { get; set; }

    public Guid? DocumentTypeId { get; set; }

    public IFormFile? File { get; set; }

    public IFormFile? CoverImage { get; set; }

    public bool RemoveCoverImage { get; set; }

    public string? OriginalFileName { get; set; }

    public string? BucketName { get; set; }

    public string? ObjectName { get; set; }

    public string? ContentType { get; set; }

    public long? FileSize { get; set; }

    public string? CoverImageFileName { get; set; }

    public string? CoverImageBucketName { get; set; }

    public string? CoverImageObjectName { get; set; }

    public string? CoverImageContentType { get; set; }

    public long? CoverImageFileSize { get; set; }

    public string? CoverImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public List<DocumentTypeSelectItemViewModel> DocumentTypes { get; set; } = new();

    public List<CongressDocumentTranslationViewModel> Translations { get; set; } = new();
}

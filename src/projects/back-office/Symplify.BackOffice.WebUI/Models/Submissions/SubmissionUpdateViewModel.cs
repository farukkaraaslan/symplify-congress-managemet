using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.AspNetCore.Http;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.WebUI.Models.Submissions;

public sealed class SubmissionUpdateViewModel
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public Guid CongressId { get; set; }

    public string CongressName { get; set; } = string.Empty;

    public string SubmissionNumber { get; set; } = string.Empty;

    [Required]
    public Guid? SubmissionTypeId { get; set; }

    public Guid? TopicId { get; set; }

    public Guid? LanguageId { get; set; }

    public string? Orcid { get; set; }

    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? TitleEn { get; set; }

    public string Abstract { get; set; } = string.Empty;

    public string? AbstractEn { get; set; }

    [MaxLength(500)]
    public string Keywords { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? KeywordsEn { get; set; }

    public bool IsSubmitted { get; set; }

    public bool CanEdit { get; set; }

    public string TransactionStatusName { get; set; } = string.Empty;

    public string TransactionStatusCode { get; set; } = string.Empty;

    public DateTime? SubmittedAt { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public bool IsExhibitionApplication { get; set; }

    [MaxLength(300)]
    public string WorkName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Dimensions { get; set; }

    [MaxLength(250)]
    public string Technique { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    [MaxLength(1000)]
    public string Address { get; set; } = string.Empty;

    public string SubmitAction { get; set; } = "draft";

    public List<SubmissionAuthorInputViewModel> Authors { get; set; } = new();

    public IReadOnlyList<SubmissionCreateSelectItemViewModel> TitleOptions { get; set; } = Array.Empty<SubmissionCreateSelectItemViewModel>();

    public IFormFile? FullTextFile { get; set; }

    public IFormFile? PresentationFile { get; set; }

    public Guid? ExistingFullTextFileId { get; set; }

    public string? ExistingFullTextFileName { get; set; }

    public string? ExistingFullTextFileContentType { get; set; }

    public long? ExistingFullTextFileSize { get; set; }

    public DateTime? ExistingFullTextFileUploadedAt { get; set; }

    public bool HasExistingFullTextFile => ExistingFullTextFileId.HasValue;

    public Guid? ExistingPresentationFileId { get; set; }

    public string? ExistingPresentationFileName { get; set; }

    public string? ExistingPresentationFileContentType { get; set; }

    public long? ExistingPresentationFileSize { get; set; }

    public DateTime? ExistingPresentationFileUploadedAt { get; set; }

    public bool HasExistingPresentationFile => ExistingPresentationFileId.HasValue;

    public IFormFile? ExhibitionFile { get; set; }

    public Guid? ExistingExhibitionFileId { get; set; }

    public SubmissionFileKind? ExistingExhibitionFileKind { get; set; }

    public string? ExistingExhibitionFileName { get; set; }

    public string? ExistingExhibitionFileContentType { get; set; }

    public long? ExistingExhibitionFileSize { get; set; }

    public DateTime? ExistingExhibitionFileUploadedAt { get; set; }

    public bool HasExistingExhibitionFile => ExistingExhibitionFileId.HasValue;

    public bool ExistingExhibitionFileIsImage
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(ExistingExhibitionFileContentType) &&
                ExistingExhibitionFileContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string extension = Path.GetExtension(ExistingExhibitionFileName ?? string.Empty);
            return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);
        }
    }

    public IReadOnlyList<SubmissionCreateSelectItemViewModel> SubmissionTypes { get; set; } = Array.Empty<SubmissionCreateSelectItemViewModel>();

    public IReadOnlyList<SubmissionCreateSelectItemViewModel> Topics { get; set; } = Array.Empty<SubmissionCreateSelectItemViewModel>();

    public IReadOnlyList<SubmissionCreateSelectItemViewModel> Languages { get; set; } = Array.Empty<SubmissionCreateSelectItemViewModel>();
}

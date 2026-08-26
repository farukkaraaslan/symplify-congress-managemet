using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Symplify.BackOffice.Application.Features.AbstractBook.Models;
using Symplify.BackOffice.Application.Features.ProgramManagement.Models;

namespace Symplify.BackOffice.WebUI.Models.AbstractBook;

public sealed class AbstractBookIndexViewModel
{
    public AbstractBookPageResponse Page { get; set; } = new();
    public AbstractBookExportViewModel Export { get; set; } = new();
}

public sealed class AbstractBookExportViewModel : IValidatableObject
{
    [Required]
    public Guid CongressId { get; set; }

    public ProgramSubmissionScopePreset SubmissionScopePreset { get; set; }
        = ProgramSubmissionScopePreset.AcceptedOnly;

    public List<string> WorkflowStatusCodes { get; set; } = new();
    public List<int> PaymentStatusIds { get; set; } = new();
    public List<Guid> SubmissionTypeIds { get; set; } = new();
    public List<Guid> TopicIds { get; set; } = new();

    [MaxLength(250)]
    public string? SubmissionSearchText { get; set; }

    public bool IncludeCover { get; set; } = true;
    public bool IncludePublicationInfo { get; set; } = true;
    public bool IncludeBoards { get; set; } = true;
    public bool IncludeTableOfContents { get; set; } = true;
    public bool StartEachSubmissionOnNewPage { get; set; } = true;
    public bool IncludeTurkishContent { get; set; } = true;
    public bool IncludeEnglishContent { get; set; } = true;
    public bool IncludeOrcid { get; set; } = true;
    public bool IncludeInstitutions { get; set; } = true;
    public bool IncludeCorrespondingAuthor { get; set; } = true;

    public AbstractBookSortMode SortMode { get; set; } = AbstractBookSortMode.SubmissionNumber;
    public AbstractBookCoverTheme CoverTheme { get; set; } = AbstractBookCoverTheme.Corporate;

    [Required]
    [MaxLength(160)]
    public string BookTitle { get; set; } = "Özet Kitabı";

    [MaxLength(160)]
    public string EnglishBookTitle { get; set; } = "Abstract Book";

    [MaxLength(200)]
    public string? Editor { get; set; }

    [MaxLength(80)]
    public string? Isbn { get; set; }

    [MaxLength(20)]
    public string? PublicationYear { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(200)]
    public string? PublishingDirector { get; set; }

    [MaxLength(200)]
    public string? CoverDesigner { get; set; }

    [MaxLength(200)]
    public string? InteriorDesigner { get; set; }

    [MaxLength(200)]
    public string? Publisher { get; set; }

    [MaxLength(250)]
    public string? EditionInformation { get; set; }

    [MaxLength(500)]
    public string? PublisherAddress { get; set; }

    [EmailAddress]
    [MaxLength(200)]
    public string? PublisherEmail { get; set; }

    [MaxLength(300)]
    public string? PublisherWebsite { get; set; }

    public IFormFile? CoverImageFile { get; set; }
    public IFormFile? HeaderLogoFile { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CongressId == Guid.Empty)
        {
            yield return new ValidationResult(
                "Kongre seçimi zorunludur.",
                new[] { nameof(CongressId) });
        }

        if (!IncludeTurkishContent && !IncludeEnglishContent)
        {
            yield return new ValidationResult(
                "En az bir içerik dili seçilmelidir.",
                new[] { nameof(IncludeTurkishContent), nameof(IncludeEnglishContent) });
        }
    }

    public ProgramSubmissionFilterDto ToFilter()
    {
        return new ProgramSubmissionFilterDto
        {
            Preset = SubmissionScopePreset,
            WorkflowStatusCodes = WorkflowStatusCodes,
            PaymentStatusIds = PaymentStatusIds,
            SubmissionTypeIds = SubmissionTypeIds,
            TopicIds = TopicIds,
            SearchText = SubmissionSearchText?.Trim()
        };
    }

    public AbstractBookOptionsDto ToOptions(
        byte[]? coverImageBytes = null,
        string? coverImageContentType = null,
        byte[]? headerLogoBytes = null,
        string? headerLogoContentType = null)
    {
        bool hasUploadedCover = coverImageBytes is { Length: > 0 };

        return new AbstractBookOptionsDto
        {
            // Kapak ayarı artık kullanıcıdan ayrı bir checkbox/tema olarak alınmıyor.
            // Gerçek bir kapak dosyası yüklendiyse ilk sayfa olarak eklenir;
            // dosya yoksa yapay/temalı kapak üretilmez.
            IncludeCover = hasUploadedCover,
            IncludePublicationInfo = false,
            IncludeBoards = IncludeBoards,
            IncludeTableOfContents = IncludeTableOfContents,
            StartEachSubmissionOnNewPage = StartEachSubmissionOnNewPage,
            IncludeTurkishContent = IncludeTurkishContent,
            IncludeEnglishContent = IncludeEnglishContent,
            IncludeOrcid = IncludeOrcid,
            IncludeInstitutions = IncludeInstitutions,
            IncludeCorrespondingAuthor = IncludeCorrespondingAuthor,
            SortMode = SortMode,
            CoverTheme = AbstractBookCoverTheme.Corporate,
            BookTitle = "Özet Kitabı",
            EnglishBookTitle = "Abstract Book",
            CoverImageBytes = coverImageBytes,
            CoverImageContentType = coverImageContentType,
            HeaderLogoBytes = headerLogoBytes,
            HeaderLogoContentType = headerLogoContentType
        };
    }
}

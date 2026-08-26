using Symplify.BackOffice.Application.Features.ProgramManagement.Models;

namespace Symplify.BackOffice.Application.Features.AbstractBook.Models;

public enum AbstractBookSortMode
{
    SubmissionNumber = 0,
    Title = 1,
    Topic = 2,
    SubmissionType = 3,
    ProgramOrder = 4
}

public enum AbstractBookCoverTheme
{
    Corporate = 0,
    Minimal = 1,
    Editorial = 2
}

public sealed class AbstractBookPageResponse
{
    public IReadOnlyList<ProgramCongressOptionDto> Congresses { get; init; } = Array.Empty<ProgramCongressOptionDto>();
    public Guid? SelectedCongressId { get; init; }
    public ProgramGenerationSourceDto? Source { get; init; }
    public string? CongressLogoUrl { get; init; }
}

public sealed class AbstractBookOptionsDto
{
    public bool IncludeCover { get; init; } = true;
    public bool IncludePublicationInfo { get; init; } = true;
    public bool IncludeBoards { get; init; } = true;
    public bool IncludeTableOfContents { get; init; } = true;
    public bool StartEachSubmissionOnNewPage { get; init; } = true;
    public bool IncludeTurkishContent { get; init; } = true;
    public bool IncludeEnglishContent { get; init; } = true;
    public bool IncludeOrcid { get; init; } = true;
    public bool IncludeInstitutions { get; init; } = true;
    public bool IncludeCorrespondingAuthor { get; init; } = true;
    public AbstractBookSortMode SortMode { get; init; } = AbstractBookSortMode.SubmissionNumber;
    public AbstractBookCoverTheme CoverTheme { get; init; } = AbstractBookCoverTheme.Corporate;

    public string BookTitle { get; init; } = "Özet Kitabı";
    public string EnglishBookTitle { get; init; } = "Abstract Book";
    public string? Editor { get; init; }
    public string? Isbn { get; init; }
    public string? PublicationYear { get; init; }
    public string? City { get; init; }

    public string? PublishingDirector { get; init; }
    public string? CoverDesigner { get; init; }
    public string? InteriorDesigner { get; init; }
    public string? Publisher { get; init; }
    public string? EditionInformation { get; init; }
    public string? PublisherAddress { get; init; }
    public string? PublisherEmail { get; init; }
    public string? PublisherWebsite { get; init; }

    public byte[]? CoverImageBytes { get; init; }
    public string? CoverImageContentType { get; init; }
    public bool CropCoverImageToFill { get; init; } = true;
    public byte[]? HeaderLogoBytes { get; set; }
    public string? HeaderLogoContentType { get; set; }
}

public sealed record AbstractBookAuthorDto(
    Guid Id,
    string DisplayName,
    string PlainName,
    string Institution,
    string? Orcid,
    string? Email,
    bool IsCorrespondingAuthor,
    int TitleOrder);

public sealed class AbstractBookSubmissionContentDto
{
    public Guid Id { get; init; }
    public string SubmissionNumber { get; init; } = string.Empty;
    public string TurkishTitle { get; init; } = string.Empty;
    public string EnglishTitle { get; init; } = string.Empty;
    public string TurkishAbstract { get; init; } = string.Empty;
    public string EnglishAbstract { get; init; } = string.Empty;
    public string TurkishKeywords { get; init; } = string.Empty;
    public string EnglishKeywords { get; init; } = string.Empty;
    public IReadOnlyList<AbstractBookAuthorDto> Authors { get; init; } = Array.Empty<AbstractBookAuthorDto>();
}

public sealed class AbstractBookDocumentSourceDto
{
    public Guid CongressId { get; init; }
    public string CongressCode { get; init; } = string.Empty;
    public string CongressName { get; init; } = string.Empty;
    public string CongressEnglishName { get; init; } = string.Empty;
    public string CongressSubtitle { get; init; } = string.Empty;
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string Venue { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public IReadOnlyList<AbstractBookSubmissionContentDto> Submissions { get; init; } = Array.Empty<AbstractBookSubmissionContentDto>();
}

public sealed class AbstractBookEntryDto
{
    public Guid Id { get; init; }
    public string SubmissionNumber { get; init; } = string.Empty;
    public string SubmissionTypeName { get; init; } = string.Empty;
    public string TopicName { get; init; } = string.Empty;
    public string TurkishTitle { get; init; } = string.Empty;
    public string EnglishTitle { get; init; } = string.Empty;
    public string TurkishAbstract { get; init; } = string.Empty;
    public string EnglishAbstract { get; init; } = string.Empty;
    public string TurkishKeywords { get; init; } = string.Empty;
    public string EnglishKeywords { get; init; } = string.Empty;
    public IReadOnlyList<AbstractBookAuthorDto> Authors { get; init; } = Array.Empty<AbstractBookAuthorDto>();
}

public sealed class AbstractBookDocumentModel
{
    public Guid CongressId { get; init; }
    public string CongressCode { get; init; } = string.Empty;
    public string CongressName { get; init; } = string.Empty;
    public string CongressEnglishName { get; init; } = string.Empty;
    public string CongressSubtitle { get; init; } = string.Empty;
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string Venue { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public AbstractBookOptionsDto Options { get; init; } = new();
    public IReadOnlyList<ProgramBoardSectionDto> Boards { get; init; } = Array.Empty<ProgramBoardSectionDto>();
    public IReadOnlyList<AbstractBookEntryDto> Entries { get; init; } = Array.Empty<AbstractBookEntryDto>();
}

public sealed record AbstractBookFileResponse(byte[] Content, string FileName);

public sealed class AbstractBookBuildRequest
{
    public Guid CongressId { get; init; }
    public string? Culture { get; init; }
    public ProgramSubmissionFilterDto Filter { get; init; } = new();
    public AbstractBookOptionsDto Options { get; init; } = new();
}

namespace Symplify.Api.Application.Features.PublicSite.Responses;

public sealed class PublicSiteBootstrapResponse
{
    public PublicOrganizationResponse Organization { get; set; } = new();
    public PublicCongressSummaryResponse Congress { get; set; } = new();
    public PublicHomeResponse Home { get; set; } = new();
    public PublicContactResponse Contact { get; set; } = new();
    public IReadOnlyCollection<PublicLanguageResponse> Languages { get; set; } = Array.Empty<PublicLanguageResponse>();
    public IReadOnlyCollection<PublicNavigationItemResponse> Navigation { get; set; } = Array.Empty<PublicNavigationItemResponse>();
    public IReadOnlyDictionary<string, string> Resources { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public sealed class PublicLocalizationResourcesResponse
{
    public string Culture { get; set; } = string.Empty;
    public IReadOnlyDictionary<string, string> Resources { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public sealed class PublicLanguageResponse
{
    public Guid Id { get; set; }
    public string Culture { get; set; } = string.Empty;
    public string TwoLetterIsoCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public int Order { get; set; }
}

public sealed class PublicHomeResponse
{
    public PublicOrganizationResponse Organization { get; set; } = new();
    public PublicCongressSummaryResponse Congress { get; set; } = new();
    public IReadOnlyCollection<PublicSliderResponse> Sliders { get; set; } = Array.Empty<PublicSliderResponse>();
    public IReadOnlyCollection<PublicAnnouncementResponse> Announcements { get; set; } = Array.Empty<PublicAnnouncementResponse>();
    public IReadOnlyCollection<PublicImportantDateResponse> ImportantDates { get; set; } = Array.Empty<PublicImportantDateResponse>();
    public IReadOnlyCollection<PublicSectionResponse> FeaturedSections { get; set; } = Array.Empty<PublicSectionResponse>();
}

public sealed class PublicOrganizationResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string? WebsiteUrl { get; set; }
    public string? HostUrl { get; set; }
    public string? LogoLightUrl { get; set; }
    public string? LogoDarkUrl { get; set; }
    public string? BrandColor { get; set; }
}

public sealed class PublicCongressSummaryResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public int? EditionNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? ShortDescription { get; set; }
    public string? WelcomeTitle { get; set; }
    public string? WelcomeContent { get; set; }
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? VenueName { get; set; }
    public string? CountryName { get; set; }
    public string? CityName { get; set; }
    public string? StateName { get; set; }
    public string? LocationText { get; set; }
    public string? LogoLightUrl { get; set; }
    public string? LogoDarkUrl { get; set; }
}

public sealed class PublicNavigationItemResponse
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int Order { get; set; }
}

public sealed class PublicSliderResponse
{
    public Guid Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string? ButtonText { get; set; }
    public string? ButtonUrl { get; set; }
    public int Order { get; set; }
}

public sealed class PublicAnnouncementResponse
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public string? ExternalUrl { get; set; }
    public string? AttachmentUrl { get; set; }
    public bool IsPinned { get; set; }
    public bool ShowInTicker { get; set; }
    public DateTime? PublishStartDate { get; set; }
    public DateTime? PublishEndDate { get; set; }
    public int Order { get; set; }
}

public sealed class PublicImportantDateResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Order { get; set; }
}

public sealed class PublicSectionResponse
{
    public Guid Id { get; set; }
    public string BindingKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public int Order { get; set; }
}

public sealed class PublicBoardsResponse
{
    public PublicCongressSummaryResponse Congress { get; set; } = new();
    public IReadOnlyCollection<PublicBoardResponse> Boards { get; set; } = Array.Empty<PublicBoardResponse>();
}

public sealed class PublicBoardResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }
    public IReadOnlyCollection<PublicBoardMemberResponse> Members { get; set; } = Array.Empty<PublicBoardMemberResponse>();
}

public sealed class PublicBoardMemberResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AcademicTitle { get; set; }
    public string? AcademicTitleShortName { get; set; }
    public string? Role { get; set; }
    public string? Institution { get; set; }
    public string? Biography { get; set; }
    public string? ImageUrl { get; set; }
    public int Order { get; set; }
}

public sealed class PublicSectionsResponse
{
    public PublicCongressSummaryResponse Congress { get; set; } = new();
    public IReadOnlyCollection<PublicSectionResponse> Sections { get; set; } = Array.Empty<PublicSectionResponse>();
}

public sealed class PublicDocumentsResponse
{
    public PublicCongressDocumentGroupResponse CurrentCongress { get; set; } = new();
    public IReadOnlyCollection<PublicCongressDocumentGroupResponse> ArchiveCongresses { get; set; } = Array.Empty<PublicCongressDocumentGroupResponse>();
}

public sealed class PublicCongressDocumentGroupResponse
{
    public Guid CongressId { get; set; }
    public string CongressTitle { get; set; } = string.Empty;
    public string? CongressCode { get; set; }
    public int? EditionNumber { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public IReadOnlyCollection<PublicDocumentResponse> Documents { get; set; } = Array.Empty<PublicDocumentResponse>();
}

public sealed class PublicDocumentResponse
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public string? DocumentTypeName { get; set; }
    public string? DocumentTypeDescription { get; set; }
    public string? DocumentTypeDisplayName { get; set; }
    public string? Description { get; set; }
    public IReadOnlyCollection<PublicDocumentTranslationResponse> Translations { get; set; } = Array.Empty<PublicDocumentTranslationResponse>();
    public string DisplayName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? ImageUrl { get; set; }
    public string? CoverImageStorageProvider { get; set; }
    public string? CoverImageBucketName { get; set; }
    public string? CoverImageObjectName { get; set; }
    public string? CoverImageFileName { get; set; }
    public string? CoverImageContentType { get; set; }
    public long? CoverImageFileSize { get; set; }
    public string? OriginalFileName { get; set; }
    public string? StorageProvider { get; set; }
    public string? BucketName { get; set; }
    public string? ObjectName { get; set; }
    public string? ContentType { get; set; }
    public string? FileExtension { get; set; }
    public long? FileSize { get; set; }
    public int Order { get; set; }
}

public sealed class PublicDocumentTranslationResponse
{
    public Guid LanguageId { get; set; }
    public string Culture { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class PublicContactResponse
{
    public Guid CongressId { get; set; }
    public string? ContactName { get; set; }
    public string? ContactTitle { get; set; }

    /// <summary>
    /// Legacy tekil e-posta alanı.
    /// Yeni portal ContactEmails listesini kullanır.
    /// Yeni çoklu e-posta yapılandırması mevcutsa yalnızca portalda görünür
    /// ana/ilk adres burada döndürülür; görünmeyen adresler fallback ile sızdırılmaz.
    /// </summary>
    public string? ContactEmail { get; set; }

    public IReadOnlyCollection<PublicContactEmailResponse> ContactEmails { get; set; }
        = Array.Empty<PublicContactEmailResponse>();

    public string? ContactPhone { get; set; }
    public string? ContactAddress { get; set; }
    public string? VenueName { get; set; }
    public string? CountryName { get; set; }

    // Geriye uyumluluk için contract'ta tutulur. Yeni kongre lokasyon modelinde kullanılmaz.
    public string? CityName { get; set; }

    public string? StateName { get; set; }
    public string? LocationText { get; set; }
}

public sealed class PublicContactEmailResponse
{
    public string Email { get; set; } = string.Empty;
    public string? Label { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsVisibleOnPortal { get; set; }
    public int Order { get; set; }
}

public sealed class PublicContentsResponse
{
    public PublicCongressSummaryResponse Congress { get; set; } = new();
    public IReadOnlyCollection<PublicLookupItemResponse> Topics { get; set; } = Array.Empty<PublicLookupItemResponse>();
    public IReadOnlyCollection<PublicLookupItemResponse> SubmissionTypes { get; set; } = Array.Empty<PublicLookupItemResponse>();
    public IReadOnlyCollection<PublicPaymentPlanResponse> PaymentPlans { get; set; } = Array.Empty<PublicPaymentPlanResponse>();
}

public sealed class PublicLookupItemResponse
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }

    // Topic kayıtlarında kongreye özel opsiyonel kategori bilgisi.
    // SubmissionType gibi diğer lookup response'larında null kalır.
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? CategoryOrder { get; set; }
}

public sealed class PublicPaymentPlanResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string AudienceType { get; set; } = string.Empty;
    public string PaymentCategory { get; set; } = string.Empty;
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public DateTime? DueDate { get; set; }
    public int Order { get; set; }
}

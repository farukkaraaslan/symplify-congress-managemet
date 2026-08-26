using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Symplify.Api.Application.Features.PublicSite.Responses;
using Symplify.Api.Application.Services.PublicSite;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Localization;
using Symplify.BackOffice.Domain.Lookups;
using Symplify.BackOffice.Domain.Reference;
using Symplify.BackOffice.Persistence.Contexts;
using OrganizationEntity = Symplify.BackOffice.Domain.Organization.Organization;

namespace Symplify.Api.Persistence.PublicSite;

public sealed class PublicSiteReadRepository : IPublicSiteReadRepository
{
    private readonly BackOfficeDbContext _dbContext;
    private readonly IPublicAssetUrlBuilder _assetUrlBuilder;

    public PublicSiteReadRepository(BackOfficeDbContext dbContext, IPublicAssetUrlBuilder assetUrlBuilder)
    {
        _dbContext = dbContext;
        _assetUrlBuilder = assetUrlBuilder;
    }

    public async Task<PublicSiteBootstrapResponse> GetBootstrapAsync(
        Guid organizationId,
        string? culture,
        CancellationToken cancellationToken)
    {
        PublicHomeResponse home = await GetHomeAsync(organizationId, culture, cancellationToken);
        PublicContactResponse contact = await GetContactAsync(organizationId, culture, cancellationToken);
        IReadOnlyCollection<PublicLanguageResponse> languages = await GetActiveLanguagesAsync(cancellationToken);

        return new PublicSiteBootstrapResponse
        {
            Organization = home.Organization,
            Congress = home.Congress,
            Home = home,
            Contact = contact,
            Languages = languages,
            Navigation = BuildNavigation(culture),
            Resources = await GetLocalizationResourcesAsync(culture, cancellationToken)
        };
    }


    public async Task<IReadOnlyDictionary<string, string>> GetLocalizationResourcesAsync(
        string? culture,
        CancellationToken cancellationToken)
    {
        LanguageContext language = await ResolveLanguageAsync(culture, cancellationToken);

        var resourceRows = await _dbContext.ResourceKeys
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(key => key.DeletedDate == null && (key.KeyName.StartsWith("Portal.") || key.KeyName.StartsWith("Common.")))
            .Select(key => new
            {
                key.KeyName,
                RequestedValue = key.Values
                    .Where(value => value.DeletedDate == null && value.LanguageId == language.RequestedLanguageId)
                    .Select(value => value.Value)
                    .FirstOrDefault(),
                DefaultValue = key.Values
                    .Where(value => value.DeletedDate == null && value.LanguageId == language.DefaultLanguageId)
                    .Select(value => value.Value)
                    .FirstOrDefault(),
                FirstValue = key.Values
                    .Where(value => value.DeletedDate == null)
                    .OrderBy(value => value.LanguageId == language.DefaultLanguageId ? 0 : 1)
                    .Select(value => value.Value)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return resourceRows
            .GroupBy(row => row.KeyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => FirstNonEmpty(
                             group.First().RequestedValue,
                             group.First().DefaultValue,
                             group.First().FirstValue,
                             group.Key) ?? group.Key,
                StringComparer.OrdinalIgnoreCase);
    }

    public async Task<PublicHomeResponse> GetHomeAsync(Guid organizationId, string? culture, CancellationToken cancellationToken)
    {
        LanguageContext language = await ResolveLanguageAsync(culture, cancellationToken);
        Congress congress = await GetCurrentCongressBaseQuery(organizationId)
            .Include(entity => entity.Organization)
            .Include(entity => entity.Country).ThenInclude(entity => entity!.Translations).ThenInclude(entity => entity.Language)
            .Include(entity => entity.State).ThenInclude(entity => entity!.Translations).ThenInclude(entity => entity.Language)
            .Include(entity => entity.Translations).ThenInclude(entity => entity.Language)
            .Include(entity => entity.Sliders).ThenInclude(entity => entity.Translations).ThenInclude(entity => entity.Language)
            .Include(entity => entity.Announcements).ThenInclude(entity => entity.Translations).ThenInclude(entity => entity.Language)
            .Include(entity => entity.ImportantDates).ThenInclude(entity => entity.Translations).ThenInclude(entity => entity.Language)
            .Include(entity => entity.Sections).ThenInclude(entity => entity.Translations).ThenInclude(entity => entity.Language)
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Published congress could not be found for this organization.");

        return new PublicHomeResponse
        {
            Organization = MapOrganization(congress.Organization),
            Congress = MapCongressSummary(congress, language),
            Sliders = congress.Sliders
                .Where(slider => slider.IsActive && slider.DeletedDate == null)
                .OrderBy(slider => NormalizeOrder(slider.Order))
                .ThenBy(slider => slider.Id)
                .Select(slider => MapSlider(slider, language))
                .ToArray(),
            Announcements = congress.Announcements
                .Where(IsPublishedAnnouncementVisible)
                .OrderByDescending(announcement => announcement.IsPinned)
                .ThenBy(announcement => NormalizeOrder(announcement.Order))
                .ThenByDescending(announcement => announcement.PublishStartDate)
                .Select(announcement => MapAnnouncement(announcement, language))
                .ToArray(),
            ImportantDates = congress.ImportantDates
                .Where(importantDate => importantDate.IsActive && importantDate.DeletedDate == null)
                .OrderBy(importantDate => NormalizeOrder(importantDate.Order))
                .ThenBy(importantDate => importantDate.StartDate)
                .Select(importantDate => MapImportantDate(importantDate, language))
                .ToArray(),
            FeaturedSections = congress.Sections
                .Where(section => section.IsActive && section.DeletedDate == null)
                .OrderBy(section => NormalizeOrder(section.Order))
                .ThenBy(section => section.BindingKey)
                .Select(section => MapSection(section, language))
                .ToArray()
        };
    }

    public async Task<PublicBoardsResponse> GetBoardsAsync(Guid organizationId, string? culture, CancellationToken cancellationToken)
    {
        LanguageContext language = await ResolveLanguageAsync(culture, cancellationToken);
        Congress congress = await GetCurrentCongressBaseQuery(organizationId)
            .Include(entity => entity.Organization)
            .Include(entity => entity.Country).ThenInclude(entity => entity!.Translations).ThenInclude(entity => entity.Language)
            .Include(entity => entity.State).ThenInclude(entity => entity!.Translations).ThenInclude(entity => entity.Language)
            .Include(entity => entity.Translations).ThenInclude(entity => entity.Language)
            .Include(entity => entity.Boards).ThenInclude(entity => entity.Translations).ThenInclude(entity => entity.Language)
            .Include(entity => entity.Boards).ThenInclude(entity => entity.Members).ThenInclude(entity => entity.Translations).ThenInclude(entity => entity.Language)
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Published congress could not be found for this organization.");

        return new PublicBoardsResponse
        {
            Congress = MapCongressSummary(congress, language),
            Boards = congress.Boards
                .Where(board => board.IsActive && board.DeletedDate == null)
                .OrderBy(board => NormalizeOrder(board.Order))
                .ThenBy(board => board.Id)
                .Select(board => MapBoard(board, language))
                .ToArray()
        };
    }

    public async Task<PublicSectionsResponse> GetSectionsAsync(Guid organizationId, string? culture, CancellationToken cancellationToken)
    {
        LanguageContext language = await ResolveLanguageAsync(culture, cancellationToken);
        Congress congress = await GetCurrentCongressBaseQuery(organizationId)
            .Include(entity => entity.Organization)
            .Include(entity => entity.Country).ThenInclude(entity => entity!.Translations).ThenInclude(entity => entity.Language)
            .Include(entity => entity.State).ThenInclude(entity => entity!.Translations).ThenInclude(entity => entity.Language)
            .Include(entity => entity.Translations).ThenInclude(entity => entity.Language)
            .Include(entity => entity.Sections).ThenInclude(entity => entity.Translations).ThenInclude(entity => entity.Language)
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Published congress could not be found for this organization.");

        return new PublicSectionsResponse
        {
            Congress = MapCongressSummary(congress, language),
            Sections = congress.Sections
                .Where(section => section.IsActive && section.DeletedDate == null)
                .OrderBy(section => NormalizeOrder(section.Order))
                .ThenBy(section => section.BindingKey)
                .Select(section => MapSection(section, language))
                .ToArray()
        };
    }

    public async Task<PublicSectionResponse?> GetSectionByBindingKeyAsync(
        Guid organizationId,
        string bindingKey,
        string? culture,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(bindingKey))
            return null;

        LanguageContext language = await ResolveLanguageAsync(culture, cancellationToken);
        Congress congress = await GetCurrentCongressBaseQuery(organizationId)
            .Include(entity => entity.Sections).ThenInclude(entity => entity.Translations).ThenInclude(entity => entity.Language)
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Published congress could not be found for this organization.");

        CongressSection? section = congress.Sections
            .Where(entity => entity.IsActive)
            .FirstOrDefault(entity => string.Equals(entity.BindingKey, bindingKey.Trim(), StringComparison.OrdinalIgnoreCase));

        return section is null ? null : MapSection(section, language);
    }

    public async Task<PublicDocumentsResponse> GetDocumentsAsync(Guid organizationId, string? culture, CancellationToken cancellationToken)
    {
        LanguageContext language = await ResolveLanguageAsync(culture, cancellationToken);
        Congress currentCongress = await GetCurrentCongressBaseQuery(organizationId)
            .Include(entity => entity.Translations).ThenInclude(entity => entity.Language)
            .Include(entity => entity.Documents).ThenInclude(entity => entity.DocumentType).ThenInclude(entity => entity!.Translations).ThenInclude(entity => entity.Language)
            .Include(entity => entity.Documents).ThenInclude(entity => entity.Translations).ThenInclude(entity => entity.Language)
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Published congress could not be found for this organization.");

        List<Congress> archiveCongresses = await _dbContext.Congresses
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == organizationId &&
                entity.Id != currentCongress.Id &&
                (entity.Status == CongressStatus.Published || entity.Status == CongressStatus.Archived) &&
                entity.DeletedDate == null &&
                entity.Documents.Any(document => document.IsActive && document.DeletedDate == null))
            .Include(entity => entity.Translations).ThenInclude(entity => entity.Language)
            .Include(entity => entity.Documents).ThenInclude(entity => entity.DocumentType).ThenInclude(entity => entity!.Translations).ThenInclude(entity => entity.Language)
            .Include(entity => entity.Documents).ThenInclude(entity => entity.Translations).ThenInclude(entity => entity.Language)
            .OrderByDescending(entity => entity.StartDate)
            .ThenByDescending(entity => entity.CreatedDate)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return new PublicDocumentsResponse
        {
            CurrentCongress = MapDocumentGroup(currentCongress, language),
            ArchiveCongresses = archiveCongresses
                .Select(congress => MapDocumentGroup(congress, language))
                .Where(group => group.Documents.Count > 0)
                .ToArray()
        };
    }

    public async Task<PublicContactResponse> GetContactAsync(Guid organizationId, string? culture, CancellationToken cancellationToken)
    {
        LanguageContext language = await ResolveLanguageAsync(culture, cancellationToken);
        Congress congress = await GetCurrentCongressBaseQuery(organizationId)
            .Include(entity => entity.Organization)
            .Include(entity => entity.ContactEmails)
            .Include(entity => entity.Country).ThenInclude(entity => entity!.Translations).ThenInclude(entity => entity.Language)
            .Include(entity => entity.State).ThenInclude(entity => entity!.Translations).ThenInclude(entity => entity.Language)
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Published congress could not be found for this organization.");

        string? countryName = ResolveCountryName(congress.Country, language);
        string? stateName = ResolveStateName(congress.State, language);

        List<CongressContactEmail> configuredContactEmails = congress.ContactEmails
            .Where(item => item.DeletedDate == null)
            .OrderByDescending(item => item.IsPrimary)
            .ThenBy(item => NormalizeOrder(item.Order))
            .ThenBy(item => item.Email)
            .ToList();

        IReadOnlyDictionary<Guid, string?> contactEmailLabels =
            await ResolveContactEmailLabelsAsync(
                configuredContactEmails,
                language,
                cancellationToken);

        PublicContactEmailResponse[] visibleContactEmails = configuredContactEmails
            .Where(item => item.IsVisibleOnPortal && !string.IsNullOrWhiteSpace(item.Email))
            .Select(item => new PublicContactEmailResponse
            {
                Email = item.Email.Trim(),
                Label = contactEmailLabels.TryGetValue(item.Id, out string? translatedLabel)
                    ? translatedLabel
                    : item.Label,
                IsPrimary = item.IsPrimary,
                IsVisibleOnPortal = true,
                Order = item.Order
            })
            .ToArray();

        // Yeni çoklu yapılandırma varsa ContactEmail yalnızca görünür adreslerden
        // seçilir. Böylece IsVisibleOnPortal=false olan bir primary adres,
        // eski portal fallback'i üzerinden istemeden dışarı açılmaz.
        string? legacyContactEmail = configuredContactEmails.Count > 0
            ? visibleContactEmails
                .OrderByDescending(item => item.IsPrimary)
                .ThenBy(item => NormalizeOrder(item.Order))
                .Select(item => item.Email)
                .FirstOrDefault()
            : FirstNonEmpty(congress.ContactEmail, congress.Organization.ContactEmail);

        // Eski kongrelerde yeni tablo henüz doldurulmamışsa tekil ContactEmail'i
        // yeni liste contract'ına da taşıyoruz.
        if (visibleContactEmails.Length == 0 &&
            configuredContactEmails.Count == 0 &&
            !string.IsNullOrWhiteSpace(legacyContactEmail))
        {
            visibleContactEmails =
            [
                new PublicContactEmailResponse
                {
                    Email = legacyContactEmail,
                    IsPrimary = true,
                    IsVisibleOnPortal = true,
                    Order = 0
                }
            ];
        }

        return new PublicContactResponse
        {
            CongressId = congress.Id,
            ContactName = FirstNonEmpty(congress.ContactName, congress.Organization.ContactName),
            ContactTitle = FirstNonEmpty(congress.ContactTitle, congress.Organization.ContactTitle),
            ContactEmail = legacyContactEmail,
            ContactEmails = visibleContactEmails,
            ContactPhone = FirstNonEmpty(congress.ContactPhone, congress.Organization.ContactPhone),
            ContactAddress = FirstNonEmpty(congress.ContactAddress, congress.Organization.ContactNote),
            VenueName = congress.VenueName,
            CountryName = countryName,
            CityName = null,
            StateName = stateName,
            LocationText = BuildLocationText(stateName, countryName)
        };
    }

    public async Task<PublicContentsResponse> GetContentsAsync(Guid organizationId, string? culture, CancellationToken cancellationToken)
    {
        LanguageContext language = await ResolveLanguageAsync(culture, cancellationToken);
        Congress congress = await GetCurrentCongressBaseQuery(organizationId)
            .Include(entity => entity.Organization)
            .Include(entity => entity.Country).ThenInclude(entity => entity!.Translations).ThenInclude(entity => entity.Language)
            .Include(entity => entity.State).ThenInclude(entity => entity!.Translations).ThenInclude(entity => entity.Language)
            .Include(entity => entity.Translations).ThenInclude(entity => entity.Language)
            .Include(entity => entity.Topics).ThenInclude(entity => entity.Topic).ThenInclude(entity => entity.Translations).ThenInclude(entity => entity.Language)
            .Include(entity => entity.Topics).ThenInclude(entity => entity.Category).ThenInclude(entity => entity!.Translations).ThenInclude(entity => entity.Language)
            .Include(entity => entity.SubmissionTypes).ThenInclude(entity => entity.SubmissionType).ThenInclude(entity => entity.Translations).ThenInclude(entity => entity.Language)
            .Include(entity => entity.PaymentPlans).ThenInclude(entity => entity.Translations).ThenInclude(entity => entity.Language)
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Published congress could not be found for this organization.");

        return new PublicContentsResponse
        {
            Congress = MapCongressSummary(congress, language),
            Topics = congress.Topics
                .Where(entity => entity.IsActive && entity.DeletedDate == null && entity.Topic.IsActive && entity.Topic.DeletedDate == null)
                .OrderBy(entity => NormalizeOrder(entity.Order))
                .ThenBy(entity => NormalizeOrder(entity.Topic.Order))
                .Select(entity => MapTopic(entity, language))
                .ToArray(),
            SubmissionTypes = congress.SubmissionTypes
                .Where(entity => entity.IsActive && entity.DeletedDate == null && entity.SubmissionType.IsActive && entity.SubmissionType.DeletedDate == null)
                .OrderBy(entity => NormalizeOrder(entity.Order))
                .ThenBy(entity => NormalizeOrder(entity.SubmissionType.Order))
                .Select(entity => MapSubmissionType(entity.SubmissionType, entity.Order, language))
                .ToArray(),
            PaymentPlans = congress.PaymentPlans
                .Where(entity => entity.IsActive && entity.IsPublicVisible && entity.DeletedDate == null)
                .OrderBy(entity => NormalizeOrder(entity.Order))
                .ThenBy(entity => entity.Code)
                .Select(entity => MapPaymentPlan(entity, language))
                .ToArray()
        };
    }

    private IQueryable<Congress> GetCurrentCongressBaseQuery(Guid organizationId)
    {
        return _dbContext.Congresses
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == organizationId &&
                entity.Status == CongressStatus.Published &&
                entity.Organization.IsActive &&
                entity.DeletedDate == null &&
                entity.Organization.DeletedDate == null)
            .OrderByDescending(entity => entity.StartDate)
            .ThenByDescending(entity => entity.CreatedDate);
    }

    private async Task<LanguageContext> ResolveLanguageAsync(string? culture, CancellationToken cancellationToken)
    {
        List<Language> languages = await _dbContext.Languages
            .AsNoTracking()
            .Where(language => language.IsActive && language.DeletedDate == null)
            .OrderByDescending(language => language.IsDefault)
            .ThenBy(language => language.Order)
            .ToListAsync(cancellationToken);

        if (languages.Count == 0)
            return new LanguageContext(Guid.Empty, string.Empty, Guid.Empty, string.Empty);

        Language defaultLanguage = languages.FirstOrDefault(language => language.IsDefault) ?? languages[0];
        Language requestedLanguage = languages.FirstOrDefault(language =>
            string.Equals(language.Culture, culture, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(language.TwoLetterIsoCode, culture, StringComparison.OrdinalIgnoreCase)) ?? defaultLanguage;

        return new LanguageContext(
            requestedLanguage.Id,
            requestedLanguage.Culture,
            defaultLanguage.Id,
            defaultLanguage.Culture);
    }

    private async Task<IReadOnlyCollection<PublicLanguageResponse>> GetActiveLanguagesAsync(CancellationToken cancellationToken)
    {
        List<Language> languages = await _dbContext.Languages
            .AsNoTracking()
            .Where(language => language.IsActive && language.DeletedDate == null)
            .OrderByDescending(language => language.IsDefault)
            .ThenBy(language => language.Order)
            .ToListAsync(cancellationToken);

        return languages
            .Select(language => new PublicLanguageResponse
            {
                Id = language.Id,
                Culture = language.Culture ?? string.Empty,
                TwoLetterIsoCode = language.TwoLetterIsoCode ?? string.Empty,
                DisplayName = FirstNonEmpty(language.Name, language.Culture, language.TwoLetterIsoCode) ?? string.Empty,
                IsDefault = language.IsDefault,
                Order = language.Order
            })
            .ToArray();
    }

    private PublicOrganizationResponse MapOrganization(OrganizationEntity organization)
    {
        return new PublicOrganizationResponse
        {
            Id = organization.Id,
            Name = organization.Name,
            Code = organization.Code,
            ShortName = organization.ShortName,
            WebsiteUrl = organization.WebsiteUrl,
            HostUrl = organization.HostUrl,
            LogoLightUrl = _assetUrlBuilder.Build(organization.LogoLightPath),
            LogoDarkUrl = _assetUrlBuilder.Build(organization.LogoDarkPath),
            BrandColor = organization.BrandColor
        };
    }

    private PublicCongressSummaryResponse MapCongressSummary(Congress congress, LanguageContext language)
    {
        CongressTranslation? translation = ResolveTranslation(congress.Translations, language);
        string? countryName = ResolveCountryName(congress.Country, language);
        string? stateName = ResolveStateName(congress.State, language);

        return new PublicCongressSummaryResponse
        {
            Id = congress.Id,
            OrganizationId = congress.OrganizationId,
            Code = congress.Code,
            Name = congress.Name,
            Slug = congress.Slug,
            EditionNumber = congress.EditionNumber,
            Title = FirstNonEmpty(translation?.Title, congress.Name) ?? congress.Name,
            Subtitle = translation?.Subtitle,
            ShortDescription = translation?.ShortDescription,
            WelcomeTitle = translation?.WelcomeTitle,
            WelcomeContent = FirstNonEmpty(translation?.WelcomeContent, translation?.Description),
            SeoTitle = translation?.SeoTitle,
            SeoDescription = translation?.SeoDescription,
            StartDate = congress.StartDate,
            EndDate = congress.EndDate,
            VenueName = congress.VenueName,
            CountryName = countryName,
            CityName = null,
            StateName = stateName,
            LocationText = BuildLocationText(stateName, countryName),
            LogoLightUrl = _assetUrlBuilder.Build(FirstNonEmpty(congress.LogoLightPath, congress.Organization?.LogoLightPath)),
            LogoDarkUrl = _assetUrlBuilder.Build(FirstNonEmpty(congress.LogoDarkPath, congress.Organization?.LogoDarkPath))
        };
    }

    private PublicSliderResponse MapSlider(CongressSlider slider, LanguageContext language)
    {
        CongressSliderTranslation? translation = ResolveTranslation(slider.Translations, language);

        return new PublicSliderResponse
        {
            Id = slider.Id,
            ImageUrl = _assetUrlBuilder.Build(slider.ImagePath) ?? slider.ImagePath,
            Title = translation?.Title,
            Subtitle = translation?.Subtitle,
            ButtonText = translation?.ButtonText,
            ButtonUrl = translation?.ButtonUrl,
            Order = slider.Order
        };
    }

    private PublicAnnouncementResponse MapAnnouncement(CongressAnnouncement announcement, LanguageContext language)
    {
        CongressAnnouncementTranslation? translation = ResolveTranslation(announcement.Translations, language);

        return new PublicAnnouncementResponse
        {
            Id = announcement.Id,
            Type = announcement.Type.ToString(),
            Title = translation?.Title ?? string.Empty,
            Summary = translation?.Summary,
            Content = translation?.Content,
            ExternalUrl = announcement.ExternalUrl,
            AttachmentUrl = _assetUrlBuilder.Build(announcement.AttachmentPath),
            IsPinned = announcement.IsPinned,
            ShowInTicker = announcement.ShowInTicker,
            PublishStartDate = announcement.PublishStartDate,
            PublishEndDate = announcement.PublishEndDate,
            Order = announcement.Order
        };
    }

    private PublicImportantDateResponse MapImportantDate(CongressImportantDate importantDate, LanguageContext language)
    {
        CongressImportantDateTranslation? translation = ResolveTranslation(importantDate.Translations, language);

        return new PublicImportantDateResponse
        {
            Id = importantDate.Id,
            Title = translation?.Title ?? string.Empty,
            Description = translation?.Description,
            StartDate = importantDate.StartDate,
            EndDate = importantDate.EndDate,
            Order = importantDate.Order
        };
    }

    private PublicSectionResponse MapSection(CongressSection section, LanguageContext language)
    {
        CongressSectionTranslation? translation = ResolveTranslation(section.Translations, language);

        return new PublicSectionResponse
        {
            Id = section.Id,
            BindingKey = section.BindingKey,
            Title = translation?.Title ?? section.BindingKey,
            Content = translation?.Content,
            Order = section.Order
        };
    }

    private PublicBoardResponse MapBoard(CongressBoard board, LanguageContext language)
    {
        CongressBoardTranslation? translation = ResolveTranslation(board.Translations, language);

        return new PublicBoardResponse
        {
            Id = board.Id,
            Name = translation?.Name ?? string.Empty,
            Description = translation?.Description,
            Order = board.Order,
            Members = board.Members
                .Where(member => member.IsActive && member.DeletedDate == null)
                .OrderBy(member => NormalizeOrder(member.Order))
                .ThenBy(member => member.FullName)
                .Select(member => MapBoardMember(member, language))
                .ToArray()
        };
    }

    private PublicBoardMemberResponse MapBoardMember(CongressBoardMember member, LanguageContext language)
    {
        CongressBoardMemberTranslation? translation = ResolveTranslation(member.Translations, language);

        // Translation.Title bu projede kurul içi görev/rol alanı olarak kullanılıyor
        // (örn. Üye, Başkan, Sekreterya). Akademik unvan değildir.
        // Akademik unvan sadece ana CongressBoardMember.AcademicTitle alanından alınmalıdır.
        string? academicTitle = FirstNonEmpty(member.AcademicTitle);
        string? role = FirstNonEmpty(translation?.Title, translation?.Biography);

        return new PublicBoardMemberResponse
        {
            Id = member.Id,
            FullName = FirstNonEmpty(translation?.FullName, member.FullName) ?? string.Empty,
            AcademicTitle = academicTitle,
            AcademicTitleShortName = ResolveAcademicTitleShortName(academicTitle),
            Role = role,
            Institution = FirstNonEmpty(translation?.Institution, member.Institution),
            Biography = translation?.Biography,
            ImageUrl = _assetUrlBuilder.Build(FirstNonEmpty(member.ImageObjectName, member.ImagePath)),
            Order = member.Order
        };
    }

    private static string? ResolveAcademicTitleShortName(string? academicTitle)
    {
        string? normalized = NormalizeTitleKey(academicTitle);

        return normalized switch
        {
            null => null,
            "profesordoktor" => "Prof. Dr.",
            "profdr" => "Prof. Dr.",
            "profesor" => "Prof.",
            "prof" => "Prof.",
            "docentdoktor" => "Doç. Dr.",
            "docdr" => "Doç. Dr.",
            "docent" => "Doç.",
            "doc" => "Doç.",
            "doktorogretimuyesi" => "Dr. Öğr. Üyesi",
            "drogretimuyesi" => "Dr. Öğr. Üyesi",
            "drogretimuy" => "Dr. Öğr. Üyesi",
            "doktor" => "Dr.",
            "dr" => "Dr.",
            "ogretimgorevlisi" => "Öğr. Gör.",
            "ogrgor" => "Öğr. Gör.",
            "arastirmagorevlisi" => "Arş. Gör.",
            "arsgor" => "Arş. Gör.",
            _ => academicTitle
        };
    }

    private static string? NormalizeTitleKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim()
            .ToLowerInvariant()
            .Replace("ı", "i")
            .Replace("İ", "i")
            .Replace("ğ", "g")
            .Replace("Ğ", "g")
            .Replace("ü", "u")
            .Replace("Ü", "u")
            .Replace("ş", "s")
            .Replace("Ş", "s")
            .Replace("ö", "o")
            .Replace("Ö", "o")
            .Replace("ç", "c")
            .Replace("Ç", "c")
            .Replace(".", string.Empty)
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty);
    }

    private PublicCongressDocumentGroupResponse MapDocumentGroup(Congress congress, LanguageContext language)
    {
        CongressTranslation? translation = ResolveTranslation(congress.Translations, language);

        return new PublicCongressDocumentGroupResponse
        {
            CongressId = congress.Id,
            CongressTitle = FirstNonEmpty(translation?.Title, congress.Name) ?? congress.Name,
            CongressCode = congress.Code,
            EditionNumber = congress.EditionNumber,
            StartDate = congress.StartDate,
            EndDate = congress.EndDate,
            Documents = congress.Documents
                .Where(document => document.IsActive && document.DeletedDate == null)
                .OrderBy(document => NormalizeOrder(document.Order))
                .ThenBy(document => document.OriginalFileName)
                .Select(document => MapDocument(document, language))
                .ToArray()
        };
    }

    private PublicDocumentResponse MapDocument(CongressDocument document, LanguageContext language)
    {
        DocumentTypeTranslation? documentTypeTranslation = document.DocumentType is null
            ? null
            : ResolveTranslation(document.DocumentType.Translations, language);

        string fallbackName = FirstNonEmpty(document.OriginalFileName, document.FilePath) ?? "Document";
        string? documentTypeName = documentTypeTranslation?.Name;
        string? documentTypeDescription = documentTypeTranslation?.Description;
        string? documentTypeDisplayName = FirstNonEmpty(documentTypeDescription, documentTypeName);
        CongressDocumentTranslation? documentTranslation = ResolveTranslation(document.Translations, language);
        string? description = documentTranslation?.Description;
        string? coverImageUrl = _assetUrlBuilder.Build(
            FirstNonEmpty(document.CoverImageObjectName, document.CoverImagePath),
            document.CoverImageBucketName);

        return new PublicDocumentResponse
        {
            Id = document.Id,
            CongressId = document.CongressId,
            DocumentTypeId = document.DocumentTypeId,
            DocumentTypeName = documentTypeName,
            DocumentTypeDescription = documentTypeDescription,
            DocumentTypeDisplayName = documentTypeDisplayName,
            Description = description,
            Translations = document.Translations
                .Where(translation => translation.DeletedDate == null && !string.IsNullOrWhiteSpace(translation.Description))
                .Select(translation => new PublicDocumentTranslationResponse
                {
                    LanguageId = translation.LanguageId,
                    Culture = translation.Language?.Culture ?? string.Empty,
                    Description = translation.Description
                })
                .ToArray(),
            DisplayName = FirstNonEmpty(documentTypeDisplayName, documentTypeName, document.OriginalFileName, Path.GetFileName(document.FilePath), "Document") ?? fallbackName,
            Url = _assetUrlBuilder.Build(FirstNonEmpty(document.ObjectName, document.FilePath), document.BucketName) ?? document.FilePath,
            CoverImageUrl = coverImageUrl,
            ThumbnailUrl = coverImageUrl,
            ImageUrl = coverImageUrl,
            CoverImageStorageProvider = document.CoverImageStorageProvider,
            CoverImageBucketName = document.CoverImageBucketName,
            CoverImageObjectName = document.CoverImageObjectName,
            CoverImageFileName = document.CoverImageFileName,
            CoverImageContentType = document.CoverImageContentType,
            CoverImageFileSize = document.CoverImageFileSize,
            OriginalFileName = document.OriginalFileName,
            StorageProvider = document.StorageProvider,
            BucketName = document.BucketName,
            ObjectName = document.ObjectName,
            ContentType = document.ContentType,
            FileExtension = document.FileExtension,
            FileSize = document.FileSize,
            Order = document.Order
        };
    }

    private PublicLookupItemResponse MapTopic(CongressTopic relation, LanguageContext language)
    {
        Topic topic = relation.Topic;
        TopicTranslation? translation = ResolveTranslation(topic.Translations, language);

        CongressTopicCategory? category = relation.Category is
        {
            IsActive: true,
            DeletedDate: null
        } && relation.Category.CongressId == relation.CongressId
            ? relation.Category
            : null;

        CongressTopicCategoryTranslation? categoryTranslation = category is null
            ? null
            : ResolveTranslation(category.Translations, language);

        return new PublicLookupItemResponse
        {
            Id = topic.Id,
            Code = topic.Code,
            Name = translation?.Name ?? topic.Code ?? string.Empty,
            Description = translation?.Description,
            Order = relation.Order,
            CategoryId = category?.Id,
            CategoryName = categoryTranslation?.Name,
            CategoryOrder = category?.Order
        };
    }

    private PublicLookupItemResponse MapSubmissionType(SubmissionType submissionType, int congressOrder, LanguageContext language)
    {
        SubmissionTypeTranslation? translation = ResolveTranslation(submissionType.Translations, language);

        return new PublicLookupItemResponse
        {
            Id = submissionType.Id,
            Code = submissionType.Code,
            Name = translation?.Name ?? submissionType.Code ?? string.Empty,
            Description = translation?.Description,
            Order = congressOrder
        };
    }

    private PublicPaymentPlanResponse MapPaymentPlan(CongressPaymentPlan paymentPlan, LanguageContext language)
    {
        CongressPaymentPlanTranslation? translation = ResolveTranslation(paymentPlan.Translations, language);

        return new PublicPaymentPlanResponse
        {
            Id = paymentPlan.Id,
            Code = paymentPlan.Code,
            Name = translation?.Name ?? paymentPlan.Code,
            Description = translation?.Description,
            Amount = paymentPlan.Amount,
            Currency = paymentPlan.Currency,
            AudienceType = paymentPlan.AudienceType,
            PaymentCategory = paymentPlan.PaymentCategory,
            ValidFrom = paymentPlan.ValidFrom,
            ValidUntil = paymentPlan.ValidUntil,
            DueDate = paymentPlan.DueDate,
            Order = paymentPlan.Order
        };
    }

    private static bool IsPublishedAnnouncementVisible(CongressAnnouncement announcement)
    {
        DateTime now = DateTime.UtcNow;

        return announcement.IsActive &&
               announcement.Status == CongressAnnouncementStatus.Published &&
               announcement.ShowOnHomePage &&
               (!announcement.PublishStartDate.HasValue || announcement.PublishStartDate.Value <= now) &&
               (!announcement.PublishEndDate.HasValue || announcement.PublishEndDate.Value >= now);
    }

    private static TTranslation? ResolveTranslation<TTranslation>(
        IEnumerable<TTranslation> translations,
        LanguageContext language)
        where TTranslation : class
    {
        return translations.FirstOrDefault(translation => HasLanguageId(translation, language.RequestedLanguageId))
               ?? translations.FirstOrDefault(translation => HasLanguageId(translation, language.DefaultLanguageId))
               ?? translations.FirstOrDefault();
    }

    private static bool HasLanguageId<TTranslation>(TTranslation translation, Guid languageId)
        where TTranslation : class
    {
        if (languageId == Guid.Empty)
            return false;

        object? value = translation.GetType().GetProperty("LanguageId")?.GetValue(translation);
        return value is Guid translationLanguageId && translationLanguageId == languageId;
    }

    private static string? ResolveCountryName(Country? country, LanguageContext language)
    {
        if (country is null)
            return null;

        var translation = ResolveTranslation(country.Translations, language);
        return translation?.Name ?? country.Code;
    }


    private static string? ResolveStateName(State? state, LanguageContext language)
    {
        if (state is null)
            return null;

        var translation = ResolveTranslation(state.Translations, language);
        return translation?.Name ?? state.Code;
    }

    private async Task<IReadOnlyDictionary<Guid, string?>> ResolveContactEmailLabelsAsync(
        IReadOnlyCollection<CongressContactEmail> contactEmails,
        LanguageContext language,
        CancellationToken cancellationToken)
    {
        Dictionary<Guid, string?> labels = contactEmails
            .ToDictionary(item => item.Id, item => FirstNonEmpty(item.Label));

        if (contactEmails.Count == 0)
            return labels;

        DbConnection connection = _dbContext.Database.GetDbConnection();
        bool closeConnection = connection.State != ConnectionState.Open;

        try
        {
            if (closeConnection)
                await connection.OpenAsync(cancellationToken);

            // BackOffice Domain'in eski sürümleri CongressContactEmailTranslation
            // entity'sini içermeyebilir. API compile-time navigation bağımlılığı
            // kurmadan tablo mevcutsa çevirileri doğrudan DB'den okur.
            await using (DbCommand tableCheckCommand = connection.CreateCommand())
            {
                tableCheckCommand.CommandText = """
                    SELECT EXISTS
                    (
                        SELECT 1
                        FROM information_schema.tables
                        WHERE table_schema = 'public'
                          AND table_name = 'CongressContactEmailTranslations'
                    );
                    """;

                object? tableExistsValue =
                    await tableCheckCommand.ExecuteScalarAsync(cancellationToken);

                if (tableExistsValue is null ||
                    tableExistsValue is DBNull ||
                    !Convert.ToBoolean(tableExistsValue))
                {
                    return labels;
                }
            }

            string[] emailParameterNames = contactEmails
                .Select((_, index) => $"@emailId{index}")
                .ToArray();

            await using DbCommand command = connection.CreateCommand();
            command.CommandText = $"""
                SELECT
                    translation."CongressContactEmailId",
                    translation."LanguageId",
                    translation."Label"
                FROM public."CongressContactEmailTranslations" AS translation
                WHERE translation."DeletedDate" IS NULL
                  AND translation."CongressContactEmailId" IN ({string.Join(", ", emailParameterNames)})
                  AND translation."LanguageId" IN (@requestedLanguageId, @defaultLanguageId);
                """;

            int parameterIndex = 0;
            foreach (CongressContactEmail contactEmail in contactEmails)
            {
                DbParameter parameter = command.CreateParameter();
                parameter.ParameterName = $"@emailId{parameterIndex++}";
                parameter.Value = contactEmail.Id;
                command.Parameters.Add(parameter);
            }

            DbParameter requestedLanguageParameter = command.CreateParameter();
            requestedLanguageParameter.ParameterName = "@requestedLanguageId";
            requestedLanguageParameter.Value = language.RequestedLanguageId;
            command.Parameters.Add(requestedLanguageParameter);

            DbParameter defaultLanguageParameter = command.CreateParameter();
            defaultLanguageParameter.ParameterName = "@defaultLanguageId";
            defaultLanguageParameter.Value = language.DefaultLanguageId;
            command.Parameters.Add(defaultLanguageParameter);

            Dictionary<Guid, Dictionary<Guid, string>> translations = new();

            await using DbDataReader reader =
                await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                Guid contactEmailId = reader.GetGuid(0);
                Guid languageId = reader.GetGuid(1);
                string label = reader.IsDBNull(2)
                    ? string.Empty
                    : reader.GetString(2).Trim();

                if (string.IsNullOrWhiteSpace(label))
                    continue;

                if (!translations.TryGetValue(
                        contactEmailId,
                        out Dictionary<Guid, string>? byLanguage))
                {
                    byLanguage = new Dictionary<Guid, string>();
                    translations[contactEmailId] = byLanguage;
                }

                byLanguage[languageId] = label;
            }

            foreach (CongressContactEmail contactEmail in contactEmails)
            {
                if (!translations.TryGetValue(
                        contactEmail.Id,
                        out Dictionary<Guid, string>? byLanguage))
                {
                    continue;
                }

                byLanguage.TryGetValue(
                    language.RequestedLanguageId,
                    out string? requestedLabel);

                byLanguage.TryGetValue(
                    language.DefaultLanguageId,
                    out string? defaultLabel);

                labels[contactEmail.Id] = FirstNonEmpty(
                    requestedLabel,
                    defaultLabel,
                    contactEmail.Label);
            }

            return labels;
        }
        finally
        {
            if (closeConnection && connection.State == ConnectionState.Open)
                await connection.CloseAsync();
        }
    }

    private static string? BuildLocationText(params string?[] parts)
    {
        string[] normalizedParts = parts
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return normalizedParts.Length == 0 ? null : string.Join(" / ", normalizedParts);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
    }

    private static int NormalizeOrder(int order)
    {
        return order <= 0 ? int.MaxValue : order;
    }

    private static IReadOnlyCollection<PublicNavigationItemResponse> BuildNavigation(string? culture)
    {
        bool isTurkish = string.IsNullOrWhiteSpace(culture) || culture.StartsWith("tr", StringComparison.OrdinalIgnoreCase);

        return new[]
        {
            new PublicNavigationItemResponse { Key = "home", Title = isTurkish ? "Ana Sayfa" : "Home", Url = "/", Order = 1 },
            new PublicNavigationItemResponse { Key = "boards", Title = isTurkish ? "Kurullar" : "Boards", Url = "/boards", Order = 2 },
            new PublicNavigationItemResponse { Key = "sections", Title = isTurkish ? "Genel Bilgiler" : "General Information", Url = "/sections", Order = 3 },
            new PublicNavigationItemResponse { Key = "contents", Title = isTurkish ? "İçerikler" : "Contents", Url = "/contents", Order = 4 },
            new PublicNavigationItemResponse { Key = "documents", Title = isTurkish ? "Dokümanlar" : "Documents", Url = "/documents", Order = 5 },
            new PublicNavigationItemResponse { Key = "contact", Title = isTurkish ? "İletişim" : "Contact", Url = "/contact", Order = 6 }
        };
    }

    private sealed record LanguageContext(
        Guid RequestedLanguageId,
        string RequestedCulture,
        Guid DefaultLanguageId,
        string DefaultCulture);
}

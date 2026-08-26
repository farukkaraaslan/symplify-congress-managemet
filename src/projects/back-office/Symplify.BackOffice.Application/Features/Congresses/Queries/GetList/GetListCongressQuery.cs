using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using Core.Application.Storage;
using MediatR;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Common.Storage;
using Symplify.BackOffice.Application.Features.Congresses.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.Congresses.Queries.GetList;

public class GetListCongressQuery : IRequest<GetListResponse<GetListCongressListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public Guid? OrganizationId { get; set; }
    public Guid? LanguageId { get; set; }
    public string? Culture { get; set; }
    public CongressStatus? Status { get; set; }
    public string? SearchText { get; set; }
    public string? SortColumn { get; set; }
    public string? SortDirection { get; set; }

    public string[] Roles => new[] { CongressesOperationClaims.Admin, CongressesOperationClaims.Read };
    public bool BypassCache => true;
    public string CacheKey => $"GetListCongresses({PageRequest.Page},{PageRequest.PageSize},{OrganizationId},{LanguageId},{Culture},{Status},{SearchText},{SortColumn},{SortDirection})";
    public string CacheGroupKey => "GetCongresses";
    public TimeSpan? SlidingExpiration { get; }

    public class GetListCongressQueryHandler : IRequestHandler<GetListCongressQuery, GetListResponse<GetListCongressListItemDto>>
    {
        private readonly ICongressRepository _repository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly ICongressTranslationRepository _translationRepository;
        private readonly IObjectStorageService _objectStorageService;
        private readonly ObjectStorageOptions _storageOptions;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public GetListCongressQueryHandler(
            ICongressRepository repository,
            IOrganizationRepository organizationRepository,
            ICongressTranslationRepository translationRepository,
            IObjectStorageService objectStorageService,
            IOptions<ObjectStorageOptions> storageOptions,
            IApplicationLanguageProvider languageProvider,
            ICurrentLanguageProvider currentLanguageProvider,
            ITranslationFallbackResolver fallbackResolver)
        {
            _repository = repository;
            _organizationRepository = organizationRepository;
            _translationRepository = translationRepository;
            _objectStorageService = objectStorageService;
            _storageOptions = storageOptions.Value;
            _languageProvider = languageProvider;
            _currentLanguageProvider = currentLanguageProvider;
            _fallbackResolver = fallbackResolver;
        }

        public async Task<GetListResponse<GetListCongressListItemDto>> Handle(GetListCongressQuery request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(request.LanguageId, request.Culture, defaultLanguage, cancellationToken);
            IReadOnlyList<ApplicationLanguageDto> languages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            Dictionary<Guid, string> cultureByLanguageId = languages.ToDictionary(language => language.Id, language => language.Culture);

            List<Congress> roots = _repository.Query().ToList();
            List<CongressTranslation> allTranslations = _translationRepository.Query().ToList();
            roots = ApplyFilters(roots, allTranslations, request);
            roots = ApplySorting(roots, allTranslations, requestedLanguage.Id, defaultLanguage.Id, request.SortColumn, request.SortDirection);

            int page = request.PageRequest.Page < 0 ? 0 : request.PageRequest.Page;
            int pageSize = request.PageRequest.PageSize <= 0 ? 20 : request.PageRequest.PageSize;
            int total = roots.Count;
            List<Congress> paged = roots.Skip(page * pageSize).Take(pageSize).ToList();
            HashSet<Guid> ids = paged.Select(root => root.Id).ToHashSet();
            HashSet<Guid> organizationIds = paged.Select(root => root.OrganizationId).ToHashSet();
            Dictionary<Guid, (string? LogoLightPath, string? LogoDarkPath)> organizationLogos = _organizationRepository.Query()
                .Where(organization => organizationIds.Contains(organization.Id))
                .Select(organization => new
                {
                    organization.Id,
                    organization.LogoLightPath,
                    organization.LogoDarkPath
                })
                .ToDictionary(
                    organization => organization.Id,
                    organization => (organization.LogoLightPath, organization.LogoDarkPath));

            List<CongressTranslation> translations = allTranslations.Where(translation => ids.Contains(translation.CongressId)).ToList();

            List<GetListCongressListItemDto> items = new();

            foreach (Congress entity in paged)
            {
                List<CongressTranslation> rootTranslations = translations.Where(translation => translation.CongressId == entity.Id).ToList();
                CongressTranslation? requestedTranslation = rootTranslations.FirstOrDefault(translation => translation.LanguageId == requestedLanguage.Id);
                CongressTranslation? displayTranslation = _fallbackResolver.Resolve(rootTranslations, requestedLanguage.Id, defaultLanguage.Id);

                GetListCongressListItemDto item = Project(
                    entity,
                    rootTranslations,
                    displayTranslation,
                    requestedTranslation is null && displayTranslation is not null,
                    cultureByLanguageId);

                organizationLogos.TryGetValue(entity.OrganizationId, out var organizationLogo);
                item.LogoLightPath = ResolveEffectiveLogoPath(item.LogoLightPath, organizationLogo.LogoLightPath);
                item.LogoDarkPath = ResolveEffectiveLogoPath(item.LogoDarkPath, organizationLogo.LogoDarkPath);
                item.LogoPath = item.LogoLightPath;
                item.LogoLightUrl = await ResolveImageUrlAsync(item.LogoLightPath, cancellationToken);
                item.LogoDarkUrl = await ResolveImageUrlAsync(item.LogoDarkPath, cancellationToken);
                item.LogoUrl = item.LogoLightUrl ?? item.LogoLightPath;
                items.Add(item);
            }

            int pages = (int)Math.Ceiling(total / (double)pageSize);
            return new GetListResponse<GetListCongressListItemDto>
            {
                Index = page,
                Size = pageSize,
                Count = total,
                Pages = pages,
                HasPrevious = page > 0,
                HasNext = page + 1 < pages,
                Items = items
            };
        }


        private static string? ResolveEffectiveLogoPath(string? congressLogoPath, string? organizationLogoPath)
        {
            return !string.IsNullOrWhiteSpace(congressLogoPath)
                ? congressLogoPath.Trim()
                : string.IsNullOrWhiteSpace(organizationLogoPath) ? null : organizationLogoPath.Trim();
        }

        private async Task<string?> ResolveImageUrlAsync(string? objectName, CancellationToken cancellationToken)
        {
            return await BackOfficeObjectStorageHelper.GetReadUrlOrPathAsync(
                _objectStorageService,
                GetCongressImagesBucketNameOrNull(),
                objectName,
                TimeSpan.FromMinutes(10),
                cancellationToken);
        }

        private string? GetCongressImagesBucketNameOrNull()
        {
            return string.IsNullOrWhiteSpace(_storageOptions.Buckets.CongressImages)
                ? null
                : _storageOptions.Buckets.CongressImages.Trim();
        }

        private async Task<ApplicationLanguageDto> ResolveRequestedLanguageAsync(Guid? languageId, string? culture, ApplicationLanguageDto defaultLanguage, CancellationToken cancellationToken)
        {
            if (languageId.HasValue)
                return await _languageProvider.GetByIdAsync(languageId.Value, cancellationToken) ?? defaultLanguage;

            if (!string.IsNullOrWhiteSpace(culture))
                return await _languageProvider.GetByCultureAsync(culture, cancellationToken) ?? defaultLanguage;

            return await _currentLanguageProvider.GetCurrentLanguageAsync(cancellationToken);
        }

        private static List<Congress> ApplyFilters(List<Congress> roots, List<CongressTranslation> translations, GetListCongressQuery request)
        {
            if (request.OrganizationId.HasValue)
                roots = roots.Where(congress => congress.OrganizationId == request.OrganizationId.Value).ToList();

            if (request.Status.HasValue)
                roots = roots.Where(congress => congress.Status == request.Status.Value).ToList();

            if (string.IsNullOrWhiteSpace(request.SearchText))
                return roots;

            string search = request.SearchText.Trim().ToLowerInvariant();
            HashSet<Guid> matchingIds = translations
                .Where(translation =>
                    (!string.IsNullOrWhiteSpace(translation.Title) && translation.Title.ToLower().Contains(search)) ||
                    (!string.IsNullOrWhiteSpace(translation.Subtitle) && translation.Subtitle.ToLower().Contains(search)) ||
                    (!string.IsNullOrWhiteSpace(translation.Description) && translation.Description.ToLower().Contains(search)) ||
                    (GetOptionalField(translation, "ShortDescription")?.ToLower().Contains(search) ?? false) ||
                    (GetOptionalField(translation, "WelcomeContent")?.ToLower().Contains(search) ?? false))
                .Select(translation => translation.CongressId)
                .ToHashSet();

            return roots.Where(congress =>
                matchingIds.Contains(congress.Id) ||
                congress.Code.ToLower().Contains(search) ||
                congress.Name.ToLower().Contains(search) ||
                (congress.ContactEmail != null && congress.ContactEmail.ToLower().Contains(search))).ToList();
        }

        private static List<Congress> ApplySorting(List<Congress> roots, List<CongressTranslation> translations, Guid requestedLanguageId, Guid defaultLanguageId, string? sortColumn, string? sortDirection)
        {
            bool descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            string column = string.IsNullOrWhiteSpace(sortColumn) ? "startdate" : sortColumn.Trim().ToLowerInvariant();

            string TitleFor(Congress congress)
            {
                CongressTranslation? requested = translations.FirstOrDefault(translation => translation.CongressId == congress.Id && translation.LanguageId == requestedLanguageId);
                CongressTranslation? fallback = requested ?? translations.FirstOrDefault(translation => translation.CongressId == congress.Id && translation.LanguageId == defaultLanguageId);
                return fallback?.Title ?? congress.Name;
            }

            IOrderedEnumerable<Congress> ordered = column switch
            {
                "title" or "name" => descending ? roots.OrderByDescending(TitleFor) : roots.OrderBy(TitleFor),
                "code" => descending ? roots.OrderByDescending(congress => congress.Code) : roots.OrderBy(congress => congress.Code),
                "status" => descending ? roots.OrderByDescending(congress => congress.Status) : roots.OrderBy(congress => congress.Status),
                "enddate" => descending ? roots.OrderByDescending(congress => congress.EndDate) : roots.OrderBy(congress => congress.EndDate),
                _ => descending ? roots.OrderByDescending(congress => congress.StartDate) : roots.OrderBy(congress => congress.StartDate)
            };

            return ordered.ThenBy(congress => congress.Id).ToList();
        }

        private static GetListCongressListItemDto Project(
            Congress entity,
            IReadOnlyCollection<CongressTranslation> translations,
            CongressTranslation? translation,
            bool isFallback,
            IReadOnlyDictionary<Guid, string> cultureByLanguageId)
        {
            return new GetListCongressListItemDto
            {
                Id = entity.Id,
                OrganizationId = entity.OrganizationId,
                Code = entity.Code,
                Name = entity.Name,
                Slug = entity.Slug,
                EditionNumber = entity.EditionNumber,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Status = entity.Status,
                ContactName = entity.ContactName,
                ContactTitle = entity.ContactTitle,
                ContactEmail = entity.ContactEmail,
                ContactPhone = entity.ContactPhone,
                ContactAddress = entity.ContactAddress,
                VenueName = entity.VenueName,
                CountryId = entity.CountryId,
                CityId = entity.CityId,
                StateId = entity.StateId,
                Title = translation?.Title ?? string.Empty,
                Subtitle = translation?.Subtitle,
                Description = translation?.Description,
                ShortDescription = GetOptionalField(translation, "ShortDescription"),
                WelcomeTitle = GetOptionalField(translation, "WelcomeTitle"),
                WelcomeContent = GetOptionalField(translation, "WelcomeContent"),
                SeoTitle = GetOptionalField(translation, "SeoTitle"),
                SeoDescription = GetOptionalField(translation, "SeoDescription"),
                LogoLightPath = entity.LogoLightPath,
                LogoDarkPath = entity.LogoDarkPath,
                LogoPath = entity.LogoLightPath,
                DisplayLanguageId = translation?.LanguageId ?? default,
                IsFallback = isFallback,
                TranslationCultures = GetTranslationCultures(translations, cultureByLanguageId)
            };
        }

        private static List<string> GetTranslationCultures(
            IEnumerable<CongressTranslation> translations,
            IReadOnlyDictionary<Guid, string> cultureByLanguageId)
        {
            return translations
                .Select(translation => cultureByLanguageId.TryGetValue(translation.LanguageId, out string? culture)
                    ? culture
                    : translation.LanguageId.ToString("N")[..8])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(culture => culture)
                .ToList();
        }

        private static string? GetOptionalField(CongressTranslation? translation, string propertyName)
            => translation is null ? null : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(translation, propertyName);
    }
}

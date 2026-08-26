using Core.Application.Pipelines.Authorization;
using Core.Application.Storage;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Common.Storage;
using Symplify.BackOffice.Application.Features.Congresses.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.Congresses.Queries.GetById;

public class GetByIdCongressQuery : IRequest<GetByIdCongressResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public Guid? LanguageId { get; set; }
    public string? Culture { get; set; }

    public string[] Roles => new[] { CongressesOperationClaims.Admin, CongressesOperationClaims.Read };

    public class GetByIdCongressQueryHandler : IRequestHandler<GetByIdCongressQuery, GetByIdCongressResponse>
    {
        private readonly ICongressRepository _repository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly ICongressTranslationRepository _translationRepository;
        private readonly IObjectStorageService _objectStorageService;
        private readonly ObjectStorageOptions _storageOptions;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public GetByIdCongressQueryHandler(
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

        public async Task<GetByIdCongressResponse> Handle(GetByIdCongressQuery request, CancellationToken cancellationToken)
        {
            Congress? entity = await _repository.GetAsync(predicate: congress => congress.Id == request.Id);
            if (entity is null)
                throw new BusinessException(CongressesMessages.EntityNotFound);

            Symplify.BackOffice.Domain.Organization.Organization? organization = await _organizationRepository.GetAsync(
                predicate: item => item.Id == entity.OrganizationId);

            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(request.LanguageId, request.Culture, defaultLanguage, cancellationToken);
            IReadOnlyList<ApplicationLanguageDto> languages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            Dictionary<Guid, string> cultureByLanguageId = languages.ToDictionary(language => language.Id, language => language.Culture);

            List<CongressTranslation> translations = _translationRepository.Query()
                .Where(translation => translation.CongressId == request.Id)
                .ToList();

            CongressTranslation? requestedTranslation = translations.FirstOrDefault(translation => translation.LanguageId == requestedLanguage.Id);
            CongressTranslation? displayTranslation = _fallbackResolver.Resolve(translations, requestedLanguage.Id, defaultLanguage.Id);

            GetByIdCongressResponse response = Project(
                entity,
                translations,
                displayTranslation,
                requestedTranslation is null && displayTranslation is not null,
                cultureByLanguageId);

            response.LogoLightPath = ResolveEffectiveLogoPath(response.LogoLightPath, organization?.LogoLightPath);
            response.LogoDarkPath = ResolveEffectiveLogoPath(response.LogoDarkPath, organization?.LogoDarkPath);
            response.LogoLightUrl = await ResolveImageUrlAsync(response.LogoLightPath, cancellationToken);
            response.LogoDarkUrl = await ResolveImageUrlAsync(response.LogoDarkPath, cancellationToken);
            response.LogoUrl = response.LogoLightUrl ?? response.LogoLightPath;

            return response;
        }


        private static string? ResolveEffectiveLogoPath(string? congressLogoPath, string? organizationLogoPath)
        {
            return !string.IsNullOrWhiteSpace(congressLogoPath)
                ? congressLogoPath.Trim()
                : string.IsNullOrWhiteSpace(organizationLogoPath) ? null : organizationLogoPath.Trim();
        }

        private async Task<ApplicationLanguageDto> ResolveRequestedLanguageAsync(Guid? languageId, string? culture, ApplicationLanguageDto defaultLanguage, CancellationToken cancellationToken)
        {
            if (languageId.HasValue)
                return await _languageProvider.GetByIdAsync(languageId.Value, cancellationToken) ?? defaultLanguage;

            if (!string.IsNullOrWhiteSpace(culture))
                return await _languageProvider.GetByCultureAsync(culture, cancellationToken) ?? defaultLanguage;

            return await _currentLanguageProvider.GetCurrentLanguageAsync(cancellationToken);
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

        private static GetByIdCongressResponse Project(
            Congress entity,
            IReadOnlyCollection<CongressTranslation> translations,
            CongressTranslation? translation,
            bool isFallback,
            IReadOnlyDictionary<Guid, string> cultureByLanguageId)
        {
            return new GetByIdCongressResponse
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

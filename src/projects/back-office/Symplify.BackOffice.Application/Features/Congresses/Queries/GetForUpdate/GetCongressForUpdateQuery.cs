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

namespace Symplify.BackOffice.Application.Features.Congresses.Queries.GetForUpdate;

public class GetCongressForUpdateQuery : IRequest<GetCongressForUpdateResponse>, ISecuredRequest
{
    public Guid Id { get; set; }

    public string[] Roles => new[] { CongressesOperationClaims.Admin, CongressesOperationClaims.Read };

    public class GetCongressForUpdateQueryHandler : IRequestHandler<GetCongressForUpdateQuery, GetCongressForUpdateResponse>
    {
        private static readonly string[] TranslationFieldNames =
        {
            "Title", "Subtitle", "Description", "ShortDescription", "WelcomeTitle", "WelcomeContent", "SeoTitle", "SeoDescription"
        };

        private readonly ICongressRepository _repository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly ICongressTranslationRepository _translationRepository;
        private readonly ICongressContactEmailRepository _contactEmailRepository;
        private readonly IObjectStorageService _objectStorageService;
        private readonly ObjectStorageOptions _storageOptions;
        private readonly IApplicationLanguageProvider _languageProvider;

        public GetCongressForUpdateQueryHandler(
            ICongressRepository repository,
            IOrganizationRepository organizationRepository,
            ICongressTranslationRepository translationRepository,
            ICongressContactEmailRepository contactEmailRepository,
            IObjectStorageService objectStorageService,
            IOptions<ObjectStorageOptions> storageOptions,
            IApplicationLanguageProvider languageProvider)
        {
            _repository = repository;
            _organizationRepository = organizationRepository;
            _translationRepository = translationRepository;
            _contactEmailRepository = contactEmailRepository;
            _objectStorageService = objectStorageService;
            _storageOptions = storageOptions.Value;
            _languageProvider = languageProvider;
        }

        public async Task<GetCongressForUpdateResponse> Handle(GetCongressForUpdateQuery request, CancellationToken cancellationToken)
        {
            Congress? entity = await _repository.GetAsync(predicate: congress => congress.Id == request.Id);
            if (entity is null)
                throw new BusinessException(CongressesMessages.EntityNotFound);

            Symplify.BackOffice.Domain.Organization.Organization? organization = await _organizationRepository.GetAsync(
                predicate: item => item.Id == entity.OrganizationId);

            string? effectiveLogoLightPath = ResolveEffectiveLogoPath(entity.LogoLightPath, organization?.LogoLightPath);
            string? effectiveLogoDarkPath = ResolveEffectiveLogoPath(entity.LogoDarkPath, organization?.LogoDarkPath);

            IReadOnlyList<ApplicationLanguageDto> languages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            List<CongressTranslation> translations = _translationRepository.Query()
                .Where(translation => translation.CongressId == request.Id)
                .ToList();

            List<CongressContactEmail> contactEmails = _contactEmailRepository.Query()
                .Where(item => item.CongressId == request.Id)
                .OrderByDescending(item => item.IsPrimary)
                .ThenBy(item => item.Order)
                .ThenBy(item => item.Email)
                .ToList();

            return new GetCongressForUpdateResponse
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
                LogoLightPath = effectiveLogoLightPath,
                LogoDarkPath = effectiveLogoDarkPath,
                LogoLightUrl = await ResolveImageUrlAsync(effectiveLogoLightPath, cancellationToken),
                LogoDarkUrl = await ResolveImageUrlAsync(effectiveLogoDarkPath, cancellationToken),
                CountryId = entity.CountryId,
                CityId = entity.CityId,
                StateId = entity.StateId,
                ContactEmails = contactEmails.Select(item => new GetCongressContactEmailForUpdateDto
                {
                    Email = item.Email,
                    Label = item.Label,
                    IsPrimary = item.IsPrimary,
                    IsVisibleOnPortal = item.IsVisibleOnPortal,
                    ReceivesContactMessages = item.ReceivesContactMessages,
                    Order = item.Order
                }).ToList(),
                Translations = languages.Select(language =>
                {
                    CongressTranslation? translation = translations.FirstOrDefault(item => item.LanguageId == language.Id);
                    return new LocalizedTranslationDto
                    {
                        LanguageId = language.Id,
                        Culture = language.Culture,
                        LanguageName = language.Name,
                        IsDefault = language.IsDefault,
                        Exists = translation is not null,
                        Fields = LocalizedEntityRuntimeHelper.ExtractFields(translation, TranslationFieldNames)
                    };
                }).ToList()
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
    }
}

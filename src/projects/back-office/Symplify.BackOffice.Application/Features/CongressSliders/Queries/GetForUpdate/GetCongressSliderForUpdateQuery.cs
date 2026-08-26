using Core.Application.Pipelines.Authorization;
using Core.Application.Storage;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Common.Storage;
using Symplify.BackOffice.Application.Features.CongressSliders.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressSliders.Queries.GetForUpdate;

public class GetCongressSliderForUpdateQuery : IRequest<GetCongressSliderForUpdateResponse>, ISecuredRequest
{
    private static readonly string[] TranslationFieldNames =
    {
        "Title",
        "Subtitle",
        "ButtonText",
        "ButtonUrl"
    };

    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public string[] Roles => new[] { CongressSlidersOperationClaims.Admin, CongressSlidersOperationClaims.Read };

    public class GetCongressSliderForUpdateQueryHandler : IRequestHandler<GetCongressSliderForUpdateQuery, GetCongressSliderForUpdateResponse>
    {
        private readonly ICongressSliderRepository _repository;
        private readonly ICongressSliderTranslationRepository _translationRepository;
        private readonly IObjectStorageService _objectStorageService;
        private readonly ObjectStorageOptions _storageOptions;
        private readonly IApplicationLanguageProvider _languageProvider;

        public GetCongressSliderForUpdateQueryHandler(
            ICongressSliderRepository repository,
            ICongressSliderTranslationRepository translationRepository,
            IObjectStorageService objectStorageService,
            IOptions<ObjectStorageOptions> storageOptions,
            IApplicationLanguageProvider languageProvider)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _objectStorageService = objectStorageService;
            _storageOptions = storageOptions.Value;
            _languageProvider = languageProvider;
        }

        public async Task<GetCongressSliderForUpdateResponse> Handle(GetCongressSliderForUpdateQuery request, CancellationToken cancellationToken)
        {
            CongressSlider? entity = await _repository.GetAsync(predicate: x => x.Id.Equals(request.Id));

            if (entity is null || (request.CongressId != Guid.Empty && entity.CongressId != request.CongressId))
                throw new BusinessException(CongressSlidersMessages.EntityNotFound);

            IReadOnlyList<ApplicationLanguageDto> languages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            List<CongressSliderTranslation> translations = _translationRepository.Query()
                .ToList()
                .Where(x => x.CongressSliderId == request.Id)
                .ToList();

            return new GetCongressSliderForUpdateResponse
            {
                Id = entity.Id,
                CongressId = entity.CongressId,
                ImagePath = entity.ImagePath,
                ImagePreviewUrl = await ResolveImagePreviewUrlAsync(entity.ImagePath, cancellationToken),
                Order = entity.Order,
                IsActive = entity.IsActive,
                Translations = languages
                    .OrderByDescending(language => language.IsDefault)
                    .ThenBy(language => language.Name)
                    .Select(language =>
                    {
                        CongressSliderTranslation? translation = translations.FirstOrDefault(x => x.LanguageId == language.Id);

                        return new LocalizedTranslationDto
                        {
                            LanguageId = language.Id,
                            Culture = language.Culture,
                            LanguageName = language.Name,
                            IsDefault = language.IsDefault,
                            Exists = translation is not null,
                            Fields = LocalizedEntityRuntimeHelper.ExtractFields(translation, TranslationFieldNames)
                        };
                    })
                    .ToList()
            };
        }

        private async Task<string?> ResolveImagePreviewUrlAsync(string? imagePath, CancellationToken cancellationToken)
        {
            return await BackOfficeObjectStorageHelper.GetReadUrlOrPathAsync(
                _objectStorageService,
                GetCongressImagesBucketNameOrNull(),
                imagePath,
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

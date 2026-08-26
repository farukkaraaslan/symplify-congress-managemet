using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Storage;
using MediatR;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Common.Storage;
using Symplify.BackOffice.Application.Features.CongressSliders.Commands;
using Symplify.BackOffice.Application.Features.CongressSliders.Constants;
using Symplify.BackOffice.Application.Features.CongressSliders.Rules;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressSliders.Commands.Update;

public class UpdateCongressSliderCommand : IRequest<UpdatedCongressSliderResponse>, ISecuredRequest, ICacheRemoverRequest
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
    public string ImagePath { get; set; } = string.Empty;
    public CongressSliderImageInputDto? Image { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressSliders";
    public string[] Roles => new[]
    {
        CongressSlidersOperationClaims.Admin,
        CongressSlidersOperationClaims.Write,
        CongressSlidersOperationClaims.Update
    };

    public class UpdateCongressSliderCommandHandler : IRequestHandler<UpdateCongressSliderCommand, UpdatedCongressSliderResponse>
    {
        private readonly ICongressRepository _congressRepository;
        private readonly ICongressSliderRepository _repository;
        private readonly ICongressSliderTranslationRepository _translationRepository;
        private readonly IObjectStorageService _objectStorageService;
        private readonly ObjectStorageOptions _storageOptions;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly IMapper _mapper;
        private readonly CongressSliderBusinessRules _rules;

        public UpdateCongressSliderCommandHandler(
            ICongressRepository congressRepository,
            ICongressSliderRepository repository,
            ICongressSliderTranslationRepository translationRepository,
            IObjectStorageService objectStorageService,
            IOptions<ObjectStorageOptions> storageOptions,
            IApplicationLanguageProvider languageProvider,
            IMapper mapper,
            CongressSliderBusinessRules rules)
        {
            _congressRepository = congressRepository;
            _repository = repository;
            _translationRepository = translationRepository;
            _objectStorageService = objectStorageService;
            _storageOptions = storageOptions.Value;
            _languageProvider = languageProvider;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<UpdatedCongressSliderResponse> Handle(UpdateCongressSliderCommand request, CancellationToken cancellationToken)
        {
            await _rules.TranslationFieldsShouldBeValid(request.Translations);

            CongressSlider? entity = await _repository.GetAsync(predicate: x => x.Id.Equals(request.Id));
            await _rules.CongressSliderShouldExistWhenSelected(entity);
            await _rules.SliderShouldBelongToCongress(entity!, request.CongressId);

            Congress? congress = await _congressRepository.GetAsync(
                predicate: item => item.Id == request.CongressId,
                cancellationToken: cancellationToken);

            if (congress is null)
                throw new Core.CrossCuttingConcerns.Exceptions.Types.BusinessException(CongressSlidersMessages.CongressNotFound);

            await _rules.ImageShouldBeValid(request.Image, isRequired: string.IsNullOrWhiteSpace(request.ImagePath) && string.IsNullOrWhiteSpace(entity!.ImagePath));

            string? oldImagePath = entity!.ImagePath;
            string? uploadedObjectName = null;

            try
            {
                string imagePath = request.Image is not null
                    ? await UploadImageAsync(congress, entity.Id, request.Image, cancellationToken)
                    : request.ImagePath.Trim();

                uploadedObjectName = request.Image is not null ? imagePath : null;
                await _rules.ImagePathShouldExist(imagePath);

                entity.ImagePath = imagePath;
                entity.IsActive = request.IsActive;

                CongressSlider updatedEntity = await _repository.UpdateAsync(entity);

                IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
                HashSet<Guid> activeLanguageIds = activeLanguages.Select(x => x.Id).ToHashSet();
                ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
                List<CongressSliderTranslation> existingTranslations = _translationRepository.Query()
                    .ToList()
                    .Where(x => x.CongressSliderId == request.Id)
                    .ToList();

                foreach (TranslationInputDto input in request.Translations)
                {
                    if (!activeLanguageIds.Contains(input.LanguageId))
                        continue;

                    bool isDefaultLanguage = input.LanguageId == defaultLanguage.Id;
                    bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(input.Fields, TranslationFieldNames);

                    if (!isDefaultLanguage && !hasAnyValue)
                        continue;

                    CongressSliderTranslation? existingTranslation = existingTranslations.FirstOrDefault(x => x.LanguageId == input.LanguageId);

                    if (existingTranslation is null)
                    {
                        CongressSliderTranslation translation = new();
                        LocalizedEntityRuntimeHelper.SetPropertyValue(translation, nameof(CongressSliderTranslation.Id), Guid.NewGuid());
                        LocalizedEntityRuntimeHelper.SetPropertyValue(translation, nameof(CongressSliderTranslation.CongressSliderId), request.Id);
                        LocalizedEntityRuntimeHelper.SetPropertyValue(translation, nameof(CongressSliderTranslation.LanguageId), input.LanguageId);
                        LocalizedEntityRuntimeHelper.ApplyFieldDictionary(translation, TranslationFieldNames, input.Fields);

                        await _translationRepository.AddAsync(translation);
                        continue;
                    }

                    LocalizedEntityRuntimeHelper.ApplyFieldDictionary(existingTranslation, TranslationFieldNames, input.Fields);
                    await _translationRepository.UpdateAsync(existingTranslation);
                }

                if (!string.IsNullOrWhiteSpace(uploadedObjectName) && !string.Equals(oldImagePath, entity.ImagePath, StringComparison.Ordinal))
                {
                    await BackOfficeObjectStorageHelper.DeleteObjectIfExistsAsync(
                        _objectStorageService,
                        GetCongressImagesBucketName(),
                        oldImagePath,
                        cancellationToken);
                }

                return _mapper.Map<UpdatedCongressSliderResponse>(updatedEntity);
            }
            catch
            {
                await BackOfficeObjectStorageHelper.DeleteObjectIfExistsAsync(
                    _objectStorageService,
                    GetCongressImagesBucketName(),
                    uploadedObjectName,
                    cancellationToken);

                throw;
            }
        }

        private async Task<string> UploadImageAsync(Congress congress, Guid sliderId, CongressSliderImageInputDto image, CancellationToken cancellationToken)
        {
            string bucketName = GetCongressImagesBucketName();
            string fileName = BackOfficeObjectStorageHelper.BuildImageFileName("congress-slider", image.OriginalFileName);
            string objectName = BackOfficeObjectStorageHelper.BuildObjectName(
                "backoffice",
                "organizations",
                congress.OrganizationId.ToString("D"),
                "congresses",
                congress.Id.ToString("D"),
                "sliders",
                sliderId.ToString("D"),
                fileName);

            ObjectStorageUploadResult uploadResult = await _objectStorageService.UploadAsync(
                new ObjectStorageUploadRequest
                {
                    BucketName = bucketName,
                    ObjectName = objectName,
                    OriginalFileName = fileName,
                    ContentType = BackOfficeObjectStorageHelper.NormalizeContentType(image.ContentType),
                    Size = image.Length,
                    Content = image.Content,
                    Metadata = new Dictionary<string, string>
                    {
                        ["module"] = "congress-sliders",
                        ["organization-id"] = congress.OrganizationId.ToString("D"),
                        ["congress-id"] = congress.Id.ToString("D"),
                        ["slider-id"] = sliderId.ToString("D")
                    }
                },
                cancellationToken);

            return uploadResult.ObjectName;
        }

        private string GetCongressImagesBucketName()
        {
            if (string.IsNullOrWhiteSpace(_storageOptions.Buckets.CongressImages))
                throw new InvalidOperationException(CongressSlidersMessages.ObjectStorageBucketMissing);

            return _storageOptions.Buckets.CongressImages.Trim();
        }
    }
}

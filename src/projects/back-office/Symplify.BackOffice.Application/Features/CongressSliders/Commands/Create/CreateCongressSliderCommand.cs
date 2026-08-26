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

namespace Symplify.BackOffice.Application.Features.CongressSliders.Commands.Create;

public class CreateCongressSliderCommand : IRequest<CreatedCongressSliderResponse>, ISecuredRequest, ICacheRemoverRequest
{
    private static readonly string[] TranslationFieldNames =
    {
        "Title",
        "Subtitle",
        "ButtonText",
        "ButtonUrl"
    };

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
        CongressSlidersOperationClaims.Add
    };

    public class CreateCongressSliderCommandHandler : IRequestHandler<CreateCongressSliderCommand, CreatedCongressSliderResponse>
    {
        private readonly ICongressRepository _congressRepository;
        private readonly ICongressSliderRepository _repository;
        private readonly ICongressSliderTranslationRepository _translationRepository;
        private readonly IObjectStorageService _objectStorageService;
        private readonly ObjectStorageOptions _storageOptions;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly IMapper _mapper;
        private readonly CongressSliderBusinessRules _rules;

        public CreateCongressSliderCommandHandler(
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

        public async Task<CreatedCongressSliderResponse> Handle(CreateCongressSliderCommand request, CancellationToken cancellationToken)
        {
            await _rules.ImageShouldBeValid(request.Image, isRequired: string.IsNullOrWhiteSpace(request.ImagePath));
            await _rules.TranslationFieldsShouldBeValid(request.Translations);

            Congress? congress = await _congressRepository.GetAsync(
                predicate: entity => entity.Id == request.CongressId,
                cancellationToken: cancellationToken);

            if (congress is null)
                throw new Core.CrossCuttingConcerns.Exceptions.Types.BusinessException(CongressSlidersMessages.CongressNotFound);

            Guid sliderId = Guid.NewGuid();
            string? uploadedObjectName = null;

            try
            {
                string imagePath = request.Image is not null
                    ? await UploadImageAsync(congress, sliderId, request.Image, cancellationToken)
                    : request.ImagePath.Trim();

                uploadedObjectName = request.Image is not null ? imagePath : null;

                await _rules.ImagePathShouldExist(imagePath);

                int nextOrder = ResolveNextOrder(request.CongressId);

                CongressSlider entity = new()
                {
                    Id = sliderId,
                    CongressId = request.CongressId,
                    ImagePath = imagePath,
                    Order = nextOrder,
                    IsActive = request.IsActive
                };

                CongressSlider createdEntity = await _repository.AddAsync(entity);

                IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
                HashSet<Guid> activeLanguageIds = activeLanguages.Select(x => x.Id).ToHashSet();
                ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);

                foreach (TranslationInputDto input in request.Translations)
                {
                    if (!activeLanguageIds.Contains(input.LanguageId))
                        continue;

                    bool isDefaultLanguage = input.LanguageId == defaultLanguage.Id;
                    bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(input.Fields, TranslationFieldNames);

                    if (!isDefaultLanguage && !hasAnyValue)
                        continue;

                    CongressSliderTranslation translation = new();
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, nameof(CongressSliderTranslation.Id), Guid.NewGuid());
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, nameof(CongressSliderTranslation.CongressSliderId), createdEntity.Id);
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, nameof(CongressSliderTranslation.LanguageId), input.LanguageId);
                    LocalizedEntityRuntimeHelper.ApplyFieldDictionary(translation, TranslationFieldNames, input.Fields);

                    await _translationRepository.AddAsync(translation);
                }

                return _mapper.Map<CreatedCongressSliderResponse>(createdEntity);
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

        private int ResolveNextOrder(Guid congressId)
        {
            return _repository.Query()
                .ToList()
                .Where(entity => entity.CongressId == congressId && !IsDeleted(entity))
                .Select(entity => entity.Order)
                .DefaultIfEmpty(0)
                .Max() + 1;
        }

        private static bool IsDeleted(object entity)
        {
            object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate");
            return deletedDate is not null;
        }
    }
}

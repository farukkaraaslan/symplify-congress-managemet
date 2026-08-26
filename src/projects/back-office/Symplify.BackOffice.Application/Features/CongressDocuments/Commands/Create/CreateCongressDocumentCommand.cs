using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Storage;
using MediatR;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressDocuments.Commands;
using Symplify.BackOffice.Application.Features.CongressDocuments.Constants;
using Symplify.BackOffice.Application.Features.CongressDocuments.Helpers;
using Symplify.BackOffice.Application.Features.CongressDocuments.Rules;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Application.Features.CongressDocuments.Commands.Create;

public class CreateCongressDocumentCommand : IRequest<CreatedCongressDocumentResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressId { get; set; }

    public Guid? DocumentTypeId { get; set; }

    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();

    public CongressDocumentFileInputDto? File { get; set; }

    public CongressDocumentFileInputDto? CoverImage { get; set; }

    public bool IsActive { get; set; } = true;

    public bool BypassCache { get; }

    public string? CacheKey { get; }

    public string CacheGroupKey => "GetCongressDocuments";

    public string[] Roles => new[]
    {
        CongressDocumentsOperationClaims.Admin,
        CongressDocumentsOperationClaims.Write,
        CongressDocumentsOperationClaims.Add
    };

    public class CreateCongressDocumentCommandHandler
        : IRequestHandler<CreateCongressDocumentCommand, CreatedCongressDocumentResponse>
    {
        private static readonly string[] TranslationFieldNames =
        {
            "Description"
        };

        private readonly ICongressDocumentRepository _repository;
        private readonly ICongressDocumentTranslationRepository _translationRepository;
        private readonly IDocumentTypeTranslationRepository _documentTypeTranslationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly IObjectStorageService _objectStorageService;
        private readonly ObjectStorageOptions _storageOptions;
        private readonly IMapper _mapper;
        private readonly CongressDocumentBusinessRules _rules;

        public CreateCongressDocumentCommandHandler(
            ICongressDocumentRepository repository,
            ICongressDocumentTranslationRepository translationRepository,
            IDocumentTypeTranslationRepository documentTypeTranslationRepository,
            IApplicationLanguageProvider languageProvider,
            IObjectStorageService objectStorageService,
            IOptions<ObjectStorageOptions> storageOptions,
            IMapper mapper,
            CongressDocumentBusinessRules rules)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _documentTypeTranslationRepository = documentTypeTranslationRepository;
            _languageProvider = languageProvider;
            _objectStorageService = objectStorageService;
            _storageOptions = storageOptions.Value;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<CreatedCongressDocumentResponse> Handle(
            CreateCongressDocumentCommand request,
            CancellationToken cancellationToken)
        {
            Congress congress = await _rules.CongressShouldExist(request.CongressId, cancellationToken);

            DocumentType documentType = await _rules.DocumentTypeShouldExist(request.DocumentTypeId, cancellationToken);
            await _rules.FileShouldBeValid(request.File, isRequired: true);
            await _rules.CoverImageShouldBeValid(request.CoverImage, isRequired: false);

            Guid documentId = Guid.NewGuid();
            CongressDocumentFileInputDto file = request.File!;
            string bucketName = GetCongressDocumentsBucketName();

            string documentTypeName = await GetDefaultDocumentTypeNameAsync(
                documentType,
                cancellationToken);

            string generatedFileName = CongressDocumentStorageNameBuilder.BuildFileName(
                congress,
                documentTypeName,
                documentId,
                file.OriginalFileName);

            string objectName = CongressDocumentStorageNameBuilder.BuildObjectName(
                congress,
                documentId,
                generatedFileName);

            ObjectStorageUploadResult uploadResult = await _objectStorageService.UploadAsync(
                new ObjectStorageUploadRequest
                {
                    BucketName = bucketName,
                    ObjectName = objectName,
                    OriginalFileName = generatedFileName,
                    ContentType = NormalizeContentType(file.ContentType),
                    Size = file.Length,
                    Content = file.Content,
                    Metadata = new Dictionary<string, string>
                    {
                        ["module"] = "congress-documents",
                        ["congress-id"] = congress.Id.ToString("N"),
                        ["organization-id"] = congress.OrganizationId.ToString("N"),
                        ["document-id"] = documentId.ToString("N"),
                        ["document-type-id"] = documentType.Id.ToString("N"),
                        ["document-type-name"] = documentTypeName
                    }
                },
                cancellationToken);

            DateTime utcNow = DateTime.UtcNow;

            CongressDocument entity = new()
            {
                Id = documentId,
                CongressId = request.CongressId,
                DocumentTypeId = request.DocumentTypeId,
                FilePath = uploadResult.ObjectName,
                OriginalFileName = generatedFileName,
                StorageProvider = _storageOptions.Provider,
                BucketName = uploadResult.BucketName,
                ObjectName = uploadResult.ObjectName,
                ContentType = uploadResult.ContentType,
                FileExtension = Path.GetExtension(generatedFileName)?.ToLowerInvariant(),
                FileSize = uploadResult.Size,
                ETag = uploadResult.ETag,
                Order = 0,
                IsActive = request.IsActive,
                CreatedDate = utcNow
            };

            if (request.CoverImage is not null && request.CoverImage.Length > 0)
                await UploadCoverImageAsync(entity, congress, request.CoverImage, cancellationToken);

            CongressDocument createdEntity = await _repository.AddAsync(entity);

            await NormalizeVisibleOrdersAsync(createdEntity, cancellationToken);
            await CreateTranslationsAsync(createdEntity.Id, request.Translations, cancellationToken);

            return _mapper.Map<CreatedCongressDocumentResponse>(createdEntity);
        }

        private async Task CreateTranslationsAsync(
            Guid documentId,
            IEnumerable<TranslationInputDto> translations,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<ApplicationLanguageDto> activeLanguages =
                await _languageProvider.GetActiveLanguagesAsync(cancellationToken);

            HashSet<Guid> activeLanguageIds = activeLanguages
                .Select(language => language.Id)
                .ToHashSet();

            foreach (TranslationInputDto input in translations)
            {
                if (!activeLanguageIds.Contains(input.LanguageId))
                    continue;

                bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(
                    input.Fields,
                    TranslationFieldNames);

                if (!hasAnyValue)
                    continue;

                CongressDocumentTranslation translation = new()
                {
                    Id = Guid.NewGuid(),
                    CongressDocumentId = documentId,
                    LanguageId = input.LanguageId,
                    Description = NormalizeOptionalText(input.Fields.TryGetValue("Description", out string? description) ? description : null),
                    CreatedDate = DateTime.UtcNow
                };

                await _translationRepository.AddAsync(translation);
            }
        }

        private async Task UploadCoverImageAsync(
            CongressDocument entity,
            Congress congress,
            CongressDocumentFileInputDto coverImage,
            CancellationToken cancellationToken)
        {
            string bucketName = GetCongressImagesBucketName();
            string generatedFileName = BuildCoverImageFileName(entity.Id, coverImage.OriginalFileName);
            string objectName = BuildCoverImageObjectName(congress.Id, entity.Id, generatedFileName);

            ObjectStorageUploadResult uploadResult = await _objectStorageService.UploadAsync(
                new ObjectStorageUploadRequest
                {
                    BucketName = bucketName,
                    ObjectName = objectName,
                    OriginalFileName = generatedFileName,
                    ContentType = NormalizeContentType(coverImage.ContentType),
                    Size = coverImage.Length,
                    Content = coverImage.Content,
                    Metadata = new Dictionary<string, string>
                    {
                        ["module"] = "congress-documents-cover-images",
                        ["congress-id"] = congress.Id.ToString("N"),
                        ["organization-id"] = congress.OrganizationId.ToString("N"),
                        ["document-id"] = entity.Id.ToString("N")
                    }
                },
                cancellationToken);

            entity.CoverImagePath = uploadResult.ObjectName;
            entity.CoverImageStorageProvider = _storageOptions.Provider;
            entity.CoverImageBucketName = uploadResult.BucketName;
            entity.CoverImageObjectName = uploadResult.ObjectName;
            entity.CoverImageFileName = uploadResult.OriginalFileName;
            entity.CoverImageContentType = uploadResult.ContentType;
            entity.CoverImageFileSize = uploadResult.Size;
            entity.CoverImageETag = uploadResult.ETag;
        }

        private async Task<string> GetDefaultDocumentTypeNameAsync(
            DocumentType documentType,
            CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage =
                await _languageProvider.GetDefaultLanguageAsync(cancellationToken);

            List<DocumentTypeTranslation> translations = _documentTypeTranslationRepository
                .Query()
                .ToList()
                .Where(translation =>
                    translation.DocumentTypeId == documentType.Id &&
                    !IsDeleted(translation))
                .ToList();

            DocumentTypeTranslation? defaultTranslation = translations
                .FirstOrDefault(translation => translation.LanguageId == defaultLanguage.Id);

            string? documentTypeName = defaultTranslation?.Name
                ?? translations.FirstOrDefault()?.Name
                ?? documentType.Code;

            return string.IsNullOrWhiteSpace(documentTypeName)
                ? "document"
                : documentTypeName;
        }

        private static string? NormalizeOptionalText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.Trim();
        }

        private string GetCongressDocumentsBucketName()
        {
            if (string.IsNullOrWhiteSpace(_storageOptions.Buckets.CongressDocuments))
                throw new InvalidOperationException(CongressDocumentsMessages.ObjectStorageBucketMissing);

            return _storageOptions.Buckets.CongressDocuments.Trim();
        }

        private string GetCongressImagesBucketName()
        {
            if (string.IsNullOrWhiteSpace(_storageOptions.Buckets.CongressImages))
                throw new InvalidOperationException(CongressDocumentsMessages.ObjectStorageBucketMissing);

            return _storageOptions.Buckets.CongressImages.Trim();
        }

        private async Task NormalizeVisibleOrdersAsync(
            CongressDocument createdEntity,
            CancellationToken cancellationToken)
        {
            List<CongressDocument> entities = _repository
                .Query()
                .ToList()
                .Where(entity =>
                    entity.CongressId == createdEntity.CongressId &&
                    !IsDeleted(entity) &&
                    entity.Id != createdEntity.Id)
                .OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                .ThenBy(entity => entity.Id)
                .ToList();

            entities.Add(createdEntity);

            await PersistNormalizedOrdersAsync(entities, cancellationToken);
        }

        private async Task PersistNormalizedOrdersAsync(
            IReadOnlyList<CongressDocument> entities,
            CancellationToken cancellationToken)
        {
            for (int index = 0; index < entities.Count; index++)
            {
                int normalizedOrder = index + 1;

                if (entities[index].Order == normalizedOrder)
                    continue;

                entities[index].Order = normalizedOrder;

                await _repository.UpdateAsync(entities[index]);
            }
        }

        private static string BuildCoverImageFileName(Guid documentId, string originalFileName)
        {
            string extension = Path.GetExtension(originalFileName);

            if (string.IsNullOrWhiteSpace(extension))
                extension = ".bin";

            return $"congress-document-cover-{documentId:N}{extension.ToLowerInvariant()}";
        }

        private static string BuildCoverImageObjectName(Guid congressId, Guid documentId, string fileName)
        {
            return string.Join(
                '/',
                "backoffice",
                "congresses",
                congressId.ToString("D"),
                "documents",
                documentId.ToString("D"),
                "cover",
                fileName);
        }

        private static string NormalizeContentType(string? contentType)
        {
            return string.IsNullOrWhiteSpace(contentType)
                ? "application/octet-stream"
                : contentType.Trim();
        }

        private static bool IsDeleted(object entity)
        {
            return LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate") is not null;
        }
    }
}

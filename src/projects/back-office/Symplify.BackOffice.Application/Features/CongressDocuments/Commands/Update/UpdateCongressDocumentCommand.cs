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

namespace Symplify.BackOffice.Application.Features.CongressDocuments.Commands.Update;

public class UpdateCongressDocumentCommand : IRequest<UpdatedCongressDocumentResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }

    public Guid CongressId { get; set; }

    public Guid? DocumentTypeId { get; set; }

    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();

    public CongressDocumentFileInputDto? File { get; set; }

    public CongressDocumentFileInputDto? CoverImage { get; set; }

    public bool RemoveCoverImage { get; set; }

    public bool IsActive { get; set; } = true;

    public bool BypassCache { get; }

    public string? CacheKey { get; }

    public string CacheGroupKey => "GetCongressDocuments";

    public string[] Roles => new[]
    {
        CongressDocumentsOperationClaims.Admin,
        CongressDocumentsOperationClaims.Write,
        CongressDocumentsOperationClaims.Update
    };

    public class UpdateCongressDocumentCommandHandler
        : IRequestHandler<UpdateCongressDocumentCommand, UpdatedCongressDocumentResponse>
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

        public UpdateCongressDocumentCommandHandler(
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

        public async Task<UpdatedCongressDocumentResponse> Handle(
            UpdateCongressDocumentCommand request,
            CancellationToken cancellationToken)
        {
            Congress congress = await _rules.CongressShouldExist(request.CongressId, cancellationToken);

            DocumentType documentType = await _rules.DocumentTypeShouldExist(request.DocumentTypeId, cancellationToken);
            await _rules.FileShouldBeValid(request.File, isRequired: false);
            await _rules.CoverImageShouldBeValid(request.CoverImage, isRequired: false);

            CongressDocument? entity = await _repository.GetAsync(
                predicate: x => x.Id.Equals(request.Id),
                cancellationToken: cancellationToken);

            await _rules.CongressDocumentShouldExistWhenSelected(entity);
            await _rules.DocumentShouldBelongToCongress(entity!, request.CongressId);

            entity!.DocumentTypeId = request.DocumentTypeId;
            entity.IsActive = request.IsActive;

            if (request.File is not null && request.File.Content != Stream.Null && request.File.Length > 0)
                await ReplaceFileAsync(entity, congress, documentType, request.File, cancellationToken);

            string? oldCoverBucketName = entity.CoverImageBucketName;
            string? oldCoverObjectName = !string.IsNullOrWhiteSpace(entity.CoverImageObjectName)
                ? entity.CoverImageObjectName
                : entity.CoverImagePath;
            bool coverChanged = false;

            if (request.RemoveCoverImage)
            {
                ClearCoverImage(entity);
                coverChanged = true;
            }
            else if (request.CoverImage is not null && request.CoverImage.Content != Stream.Null && request.CoverImage.Length > 0)
            {
                await ReplaceCoverImageAsync(entity, congress, request.CoverImage, cancellationToken);
                coverChanged = true;
            }

            await NormalizeVisibleOrdersAsync(entity, cancellationToken);

            CongressDocument updatedEntity = await _repository.UpdateAsync(entity);

            await UpsertTranslationsAsync(
                request.Id,
                request.Translations,
                cancellationToken);

            if (coverChanged)
                await DeleteCoverImageIfExistsAsync(oldCoverBucketName, oldCoverObjectName, cancellationToken);

            return _mapper.Map<UpdatedCongressDocumentResponse>(updatedEntity);
        }

        private async Task UpsertTranslationsAsync(
            Guid documentId,
            IEnumerable<TranslationInputDto> translations,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<ApplicationLanguageDto> activeLanguages =
                await _languageProvider.GetActiveLanguagesAsync(cancellationToken);

            HashSet<Guid> activeLanguageIds = activeLanguages
                .Select(language => language.Id)
                .ToHashSet();

            List<CongressDocumentTranslation> existingTranslations = _translationRepository
                .Query()
                .ToList()
                .Where(translation =>
                    translation.CongressDocumentId == documentId &&
                    !IsDeleted(translation))
                .ToList();

            foreach (TranslationInputDto input in translations)
            {
                if (!activeLanguageIds.Contains(input.LanguageId))
                    continue;

                bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(
                    input.Fields,
                    TranslationFieldNames);

                CongressDocumentTranslation? existingTranslation = existingTranslations
                    .FirstOrDefault(translation => translation.LanguageId == input.LanguageId);

                string? description = NormalizeOptionalText(
                    input.Fields.TryGetValue("Description", out string? value) ? value : null);

                if (!hasAnyValue)
                {
                    if (existingTranslation is not null)
                    {
                        existingTranslation.Description = null;
                        await _translationRepository.UpdateAsync(existingTranslation);
                    }

                    continue;
                }

                if (existingTranslation is null)
                {
                    CongressDocumentTranslation translation = new()
                    {
                        Id = Guid.NewGuid(),
                        CongressDocumentId = documentId,
                        LanguageId = input.LanguageId,
                        Description = description,
                        CreatedDate = DateTime.UtcNow
                    };

                    await _translationRepository.AddAsync(translation);
                    continue;
                }

                existingTranslation.Description = description;
                await _translationRepository.UpdateAsync(existingTranslation);
            }
        }

        private static string? NormalizeOptionalText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.Trim();
        }

        private async Task ReplaceFileAsync(
            CongressDocument entity,
            Congress congress,
            DocumentType documentType,
            CongressDocumentFileInputDto file,
            CancellationToken cancellationToken)
        {
            string bucketName = GetCongressDocumentsBucketName();

            string documentTypeName = await GetDefaultDocumentTypeNameAsync(
                documentType,
                cancellationToken);

            string generatedFileName = CongressDocumentStorageNameBuilder.BuildFileName(
                congress,
                documentTypeName,
                entity.Id,
                file.OriginalFileName);

            string objectName = CongressDocumentStorageNameBuilder.BuildObjectName(
                congress,
                entity.Id,
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
                        ["document-id"] = entity.Id.ToString("N"),
                        ["document-type-id"] = documentType.Id.ToString("N"),
                        ["document-type-name"] = documentTypeName
                    }
                },
                cancellationToken);

            entity.FilePath = uploadResult.ObjectName;
            entity.OriginalFileName = generatedFileName;
            entity.StorageProvider = _storageOptions.Provider;
            entity.BucketName = uploadResult.BucketName;
            entity.ObjectName = uploadResult.ObjectName;
            entity.ContentType = uploadResult.ContentType;
            entity.FileExtension = Path.GetExtension(generatedFileName)?.ToLowerInvariant();
            entity.FileSize = uploadResult.Size;
            entity.ETag = uploadResult.ETag;
        }

        private async Task ReplaceCoverImageAsync(
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

        private static void ClearCoverImage(CongressDocument entity)
        {
            entity.CoverImagePath = null;
            entity.CoverImageStorageProvider = null;
            entity.CoverImageBucketName = null;
            entity.CoverImageObjectName = null;
            entity.CoverImageFileName = null;
            entity.CoverImageContentType = null;
            entity.CoverImageFileSize = null;
            entity.CoverImageETag = null;
        }

        private async Task DeleteCoverImageIfExistsAsync(string? bucketName, string? objectName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(bucketName) ||
                string.IsNullOrWhiteSpace(objectName) ||
                IsExternalOrLegacyLocalPath(objectName))
            {
                return;
            }

            try
            {
                await _objectStorageService.DeleteAsync(
                    new ObjectStorageDeleteRequest
                    {
                        BucketName = bucketName.Trim(),
                        ObjectName = objectName.Trim()
                    },
                    cancellationToken);
            }
            catch
            {
                // Old cover cleanup is best-effort. The document update should not fail for an already removed object.
            }
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
            CongressDocument currentEntity,
            CancellationToken cancellationToken)
        {
            List<CongressDocument> entities = _repository
                .Query()
                .ToList()
                .Where(entity =>
                    entity.CongressId == currentEntity.CongressId &&
                    !IsDeleted(entity) &&
                    entity.Id != currentEntity.Id)
                .OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                .ThenBy(entity => entity.Id)
                .ToList();

            int targetOrder = currentEntity.Order <= 0
                ? entities.Count + 1
                : Math.Clamp(currentEntity.Order, 1, entities.Count + 1);

            entities.Insert(targetOrder - 1, currentEntity);

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

        private static bool IsExternalOrLegacyLocalPath(string path)
        {
            return path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("/", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("~/", StringComparison.OrdinalIgnoreCase);
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

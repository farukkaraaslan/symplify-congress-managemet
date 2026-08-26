using Core.Application.Pipelines.Authorization;
using Core.Application.Storage;
using MediatR;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Constants;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Rules;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressBoardMembers.Queries.GetForUpdate;

public class GetCongressBoardMemberForUpdateQuery : IRequest<GetCongressBoardMemberForUpdateResponse>, ISecuredRequest
{
    public Guid Id { get; set; }

    public Guid CongressId { get; set; }

    public string[] Roles => new[] { CongressBoardMembersOperationClaims.Admin, CongressBoardMembersOperationClaims.Read };

    public class GetCongressBoardMemberForUpdateQueryHandler : IRequestHandler<GetCongressBoardMemberForUpdateQuery, GetCongressBoardMemberForUpdateResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Biography" };

        private readonly ICongressBoardMemberRepository _repository;
        private readonly ICongressBoardRepository _boardRepository;
        private readonly ICongressBoardTranslationRepository _boardTranslationRepository;
        private readonly ICongressBoardMemberTranslationRepository _translationRepository;
        private readonly IObjectStorageService _objectStorageService;
        private readonly ObjectStorageOptions _storageOptions;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;
        private readonly CongressBoardMemberBusinessRules _rules;

        public GetCongressBoardMemberForUpdateQueryHandler(
            ICongressBoardMemberRepository repository,
            ICongressBoardRepository boardRepository,
            ICongressBoardTranslationRepository boardTranslationRepository,
            ICongressBoardMemberTranslationRepository translationRepository,
            IObjectStorageService objectStorageService,
            IOptions<ObjectStorageOptions> storageOptions,
            IApplicationLanguageProvider languageProvider,
            ICurrentLanguageProvider currentLanguageProvider,
            ITranslationFallbackResolver fallbackResolver,
            CongressBoardMemberBusinessRules rules)
        {
            _repository = repository;
            _boardRepository = boardRepository;
            _boardTranslationRepository = boardTranslationRepository;
            _translationRepository = translationRepository;
            _objectStorageService = objectStorageService;
            _storageOptions = storageOptions.Value;
            _languageProvider = languageProvider;
            _currentLanguageProvider = currentLanguageProvider;
            _fallbackResolver = fallbackResolver;
            _rules = rules;
        }

        public async Task<GetCongressBoardMemberForUpdateResponse> Handle(
            GetCongressBoardMemberForUpdateQuery request,
            CancellationToken cancellationToken)
        {
            CongressBoardMember? entity = _repository
                .Query()
                .ToList()
                .FirstOrDefault(member => member.Id == request.Id && !IsDeleted(member));

            await _rules.CongressBoardMemberShouldExistWhenSelected(entity);

            CongressBoard? board = _boardRepository
                .Query()
                .ToList()
                .FirstOrDefault(item => item.Id == entity!.CongressBoardId && !IsDeleted(item));

            if (request.CongressId != Guid.Empty && board is not null && board.CongressId != request.CongressId)
                throw new InvalidOperationException(CongressBoardMembersMessages.EntityNotFound);

            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await _currentLanguageProvider.GetCurrentLanguageAsync(cancellationToken);

            string boardName = ResolveBoardName(entity!.CongressBoardId, requestedLanguage.Id, defaultLanguage.Id);

            List<CongressBoardMemberTranslation> existingTranslations = _translationRepository
                .Query()
                .ToList()
                .Where(translation => translation.CongressBoardMemberId == entity.Id && !IsDeleted(translation))
                .ToList();

            IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);

            List<LocalizedTranslationDto> translations = activeLanguages
                .OrderByDescending(language => language.IsDefault)
                .ThenBy(language => language.Order)
                .ThenBy(language => language.Name)
                .Select(language =>
                {
                    CongressBoardMemberTranslation? translation = existingTranslations.FirstOrDefault(item => item.LanguageId == language.Id);

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
                .ToList();

            return new GetCongressBoardMemberForUpdateResponse
            {
                Id = entity.Id,
                CongressId = board?.CongressId ?? request.CongressId,
                CongressBoardId = entity.CongressBoardId,
                BoardName = boardName,
                FullName = entity.FullName,
                AcademicTitle = entity.AcademicTitle,
                Institution = entity.Institution,
                ImagePath = entity.ImagePath,
                ImagePreviewUrl = await GetImagePreviewUrlAsync(entity, cancellationToken),
                ImageBucketName = entity.ImageBucketName,
                ImageObjectName = entity.ImageObjectName,
                ImageFileName = entity.ImageFileName,
                ImageContentType = entity.ImageContentType,
                ImageFileSize = entity.ImageFileSize,
                IsAcceptanceLetterSigner = entity.IsAcceptanceLetterSigner,
                SignaturePath = entity.SignaturePath,
                SignaturePreviewUrl = await GetSignaturePreviewUrlAsync(entity, cancellationToken),
                SignatureBucketName = entity.SignatureBucketName,
                SignatureObjectName = entity.SignatureObjectName,
                SignatureFileName = entity.SignatureFileName,
                SignatureContentType = entity.SignatureContentType,
                SignatureFileSize = entity.SignatureFileSize,
                Order = entity.Order,
                IsActive = entity.IsActive,
                Translations = translations
            };
        }

        private string ResolveBoardName(Guid boardId, Guid requestedLanguageId, Guid defaultLanguageId)
        {
            List<CongressBoardTranslation> boardTranslations = _boardTranslationRepository
                .Query()
                .ToList()
                .Where(translation => translation.CongressBoardId == boardId && !IsDeleted(translation))
                .ToList();

            CongressBoardTranslation? translation = _fallbackResolver.Resolve(boardTranslations, requestedLanguageId, defaultLanguageId);

            return translation?.Name ?? string.Empty;
        }

        private async Task<string?> GetImagePreviewUrlAsync(CongressBoardMember entity, CancellationToken cancellationToken)
        {
            string? objectName = !string.IsNullOrWhiteSpace(entity.ImageObjectName)
                ? entity.ImageObjectName
                : entity.ImagePath;

            if (string.IsNullOrWhiteSpace(objectName))
                return null;

            if (IsExternalOrLegacyLocalPath(objectName))
                return objectName;

            string bucketName = !string.IsNullOrWhiteSpace(entity.ImageBucketName)
                ? entity.ImageBucketName.Trim()
                : GetCongressImagesBucketName();

            try
            {
                return await _objectStorageService.GetPresignedReadUrlAsync(
                    bucketName,
                    objectName.Trim(),
                    TimeSpan.FromMinutes(10),
                    cancellationToken);
            }
            catch
            {
                return null;
            }
        }


        private async Task<string?> GetSignaturePreviewUrlAsync(CongressBoardMember entity, CancellationToken cancellationToken)
        {
            string? objectName = !string.IsNullOrWhiteSpace(entity.SignatureObjectName)
                ? entity.SignatureObjectName
                : entity.SignaturePath;

            if (string.IsNullOrWhiteSpace(objectName))
                return null;

            if (IsExternalOrLegacyLocalPath(objectName))
                return objectName;

            string bucketName = !string.IsNullOrWhiteSpace(entity.SignatureBucketName)
                ? entity.SignatureBucketName.Trim()
                : GetCongressImagesBucketName();

            try
            {
                return await _objectStorageService.GetPresignedReadUrlAsync(
                    bucketName,
                    objectName.Trim(),
                    TimeSpan.FromMinutes(10),
                    cancellationToken);
            }
            catch
            {
                return null;
            }
        }

        private string GetCongressImagesBucketName()
        {
            if (string.IsNullOrWhiteSpace(_storageOptions.Buckets.CongressImages))
                throw new InvalidOperationException(CongressBoardMembersMessages.ObjectStorageBucketMissing);

            return _storageOptions.Buckets.CongressImages.Trim();
        }

        private static bool IsExternalOrLegacyLocalPath(string path)
        {
            return path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("/", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("~/", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDeleted(object entity)
            => LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate") is not null;
    }
}

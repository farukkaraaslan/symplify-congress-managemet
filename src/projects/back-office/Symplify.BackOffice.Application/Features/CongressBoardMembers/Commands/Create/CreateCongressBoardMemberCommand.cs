using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Storage;
using MediatR;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Constants;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Rules;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands.Create;

public class CreateCongressBoardMemberCommand : IRequest<CreatedCongressBoardMemberResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressId { get; set; }

    public Guid? CongressBoardId { get; set; }

    public string? BoardName { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? AcademicTitle { get; set; }

    public string? Institution { get; set; }

    public CongressBoardMemberImageInputDto? Image { get; set; }

    public int Order { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressBoardMembers";
    public string[] Roles => new[] { CongressBoardMembersOperationClaims.Admin, CongressBoardMembersOperationClaims.Write, CongressBoardMembersOperationClaims.Add };

    public class CreateCongressBoardMemberCommandHandler : IRequestHandler<CreateCongressBoardMemberCommand, CreatedCongressBoardMemberResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Biography" };

        private readonly ICongressBoardRepository _boardRepository;
        private readonly ICongressBoardTranslationRepository _boardTranslationRepository;
        private readonly ICongressBoardMemberRepository _repository;
        private readonly ICongressBoardMemberTranslationRepository _translationRepository;
        private readonly IObjectStorageService _objectStorageService;
        private readonly ObjectStorageOptions _storageOptions;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly IMapper _mapper;
        private readonly CongressBoardMemberBusinessRules _rules;

        public CreateCongressBoardMemberCommandHandler(
            ICongressBoardRepository boardRepository,
            ICongressBoardTranslationRepository boardTranslationRepository,
            ICongressBoardMemberRepository repository,
            ICongressBoardMemberTranslationRepository translationRepository,
            IObjectStorageService objectStorageService,
            IOptions<ObjectStorageOptions> storageOptions,
            IApplicationLanguageProvider languageProvider,
            IMapper mapper,
            CongressBoardMemberBusinessRules rules)
        {
            _boardRepository = boardRepository;
            _boardTranslationRepository = boardTranslationRepository;
            _repository = repository;
            _translationRepository = translationRepository;
            _objectStorageService = objectStorageService;
            _storageOptions = storageOptions.Value;
            _languageProvider = languageProvider;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<CreatedCongressBoardMemberResponse> Handle(
            CreateCongressBoardMemberCommand request,
            CancellationToken cancellationToken)
        {
            await _rules.CongressShouldBeSelected(request.CongressId);
            await _rules.FullNameShouldNotBeEmpty(request.FullName);
            await _rules.ImageShouldBeValid(request.Image);

            CongressBoard board = await EnsureBoardAsync(request, cancellationToken);

            int order = GetNextOrder(board.Id);
            Guid memberId = Guid.NewGuid();

            CongressBoardMember entity = new()
            {
                Id = memberId,
                CongressBoardId = board.Id,
                FullName = request.FullName.Trim(),
                AcademicTitle = Normalize(request.AcademicTitle),
                Institution = Normalize(request.Institution),
                Order = order,
                IsActive = request.IsActive
            };

            if (request.Image is not null && request.Image.Length > 0)
                await UploadImageAsync(entity, board.CongressId, request.Image, cancellationToken);

            CongressBoardMember createdEntity = await _repository.AddAsync(entity);
            await UpsertTranslationsAsync(createdEntity, request.Translations, cancellationToken);

            return _mapper.Map<CreatedCongressBoardMemberResponse>(createdEntity);
        }

        private async Task UploadImageAsync(
            CongressBoardMember entity,
            Guid congressId,
            CongressBoardMemberImageInputDto image,
            CancellationToken cancellationToken)
        {
            string bucketName = GetCongressImagesBucketName();
            string generatedFileName = BuildImageFileName(entity.Id, image.OriginalFileName);
            string objectName = BuildImageObjectName(congressId, entity.Id, generatedFileName);

            ObjectStorageUploadResult uploadResult = await _objectStorageService.UploadAsync(
                new ObjectStorageUploadRequest
                {
                    BucketName = bucketName,
                    ObjectName = objectName,
                    OriginalFileName = generatedFileName,
                    ContentType = NormalizeContentType(image.ContentType),
                    Size = image.Length,
                    Content = image.Content,
                    Metadata = new Dictionary<string, string>
                    {
                        ["module"] = "congress-board-members",
                        ["congress-id"] = congressId.ToString("D"),
                        ["board-member-id"] = entity.Id.ToString("D")
                    }
                },
                cancellationToken);

            entity.ImagePath = uploadResult.ObjectName;
            entity.ImageStorageProvider = _storageOptions.Provider;
            entity.ImageBucketName = uploadResult.BucketName;
            entity.ImageObjectName = uploadResult.ObjectName;
            entity.ImageFileName = uploadResult.OriginalFileName;
            entity.ImageContentType = uploadResult.ContentType;
            entity.ImageFileSize = uploadResult.Size;
            entity.ImageETag = uploadResult.ETag;
        }

        private async Task<CongressBoard> EnsureBoardAsync(
            CreateCongressBoardMemberCommand request,
            CancellationToken cancellationToken)
        {
            List<CongressBoard> boards = _boardRepository
                .Query()
                .ToList()
                .Where(board => board.CongressId == request.CongressId && !IsDeleted(board))
                .ToList();

            if (request.CongressBoardId.HasValue && request.CongressBoardId.Value != Guid.Empty)
            {
                CongressBoard? existingBoard = boards.FirstOrDefault(board => board.Id == request.CongressBoardId.Value);

                if (existingBoard is not null)
                    return existingBoard;
            }

            if (string.IsNullOrWhiteSpace(request.BoardName))
                throw new InvalidOperationException(CongressBoardMembersMessages.BoardRequired);

            string boardName = request.BoardName.Trim();
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            HashSet<Guid> boardIds = boards.Select(board => board.Id).ToHashSet();

            List<CongressBoardTranslation> boardTranslations = _boardTranslationRepository
                .Query()
                .ToList()
                .Where(translation => boardIds.Contains(translation.CongressBoardId) && !IsDeleted(translation))
                .ToList();

            CongressBoardTranslation? matchedTranslation = boardTranslations
                .FirstOrDefault(translation =>
                    translation.LanguageId == defaultLanguage.Id &&
                    string.Equals(translation.Name?.Trim(), boardName, StringComparison.OrdinalIgnoreCase))
                ?? boardTranslations.FirstOrDefault(translation =>
                    string.Equals(translation.Name?.Trim(), boardName, StringComparison.OrdinalIgnoreCase));

            CongressBoard? matchedBoard = matchedTranslation is null
                ? null
                : boards.FirstOrDefault(board => board.Id == matchedTranslation.CongressBoardId);

            if (matchedBoard is not null)
                return matchedBoard;

            throw new InvalidOperationException(CongressBoardMembersMessages.BoardRequired);
        }

        private async Task UpsertTranslationsAsync(
            CongressBoardMember entity,
            IEnumerable<TranslationInputDto> translations,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            HashSet<Guid> activeLanguageIds = activeLanguages.Select(language => language.Id).ToHashSet();

            List<TranslationInputDto> requestedTranslations = translations
                .Where(input => activeLanguageIds.Contains(input.LanguageId))
                .GroupBy(input => input.LanguageId)
                .Select(group => group.First())
                .ToList();

            if (requestedTranslations.All(input => input.LanguageId != defaultLanguage.Id))
            {
                requestedTranslations.Add(new TranslationInputDto
                {
                    LanguageId = defaultLanguage.Id,
                    Fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                });
            }

            foreach (TranslationInputDto input in requestedTranslations)
            {
                bool isDefaultLanguage = input.LanguageId == defaultLanguage.Id;
                bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(input.Fields, TranslationFieldNames);

                if (!isDefaultLanguage && !hasAnyValue)
                    continue;

                CongressBoardMemberTranslation translation = new()
                {
                    Id = Guid.NewGuid(),
                    CongressBoardMemberId = entity.Id,
                    LanguageId = input.LanguageId,
                    FullName = entity.FullName,
                    Title = entity.AcademicTitle,
                    Institution = entity.Institution,
                    Biography = GetField(input.Fields, "Biography")
                };

                await _translationRepository.AddAsync(translation);
            }
        }

        private int GetNextOrder(Guid boardId)
        {
            List<CongressBoardMember> members = _repository
                .Query()
                .ToList()
                .Where(member => member.CongressBoardId == boardId && !IsDeleted(member))
                .ToList();

            return members.Count == 0
                ? 1
                : members.Max(member => member.Order <= 0 ? 0 : member.Order) + 1;
        }

        private string GetCongressImagesBucketName()
        {
            if (string.IsNullOrWhiteSpace(_storageOptions.Buckets.CongressImages))
                throw new InvalidOperationException(CongressBoardMembersMessages.ObjectStorageBucketMissing);

            return _storageOptions.Buckets.CongressImages.Trim();
        }

        private static string BuildImageFileName(Guid memberId, string originalFileName)
        {
            string extension = Path.GetExtension(originalFileName);
            return $"board-member-{memberId:N}{extension.ToLowerInvariant()}";
        }

        private static string BuildImageObjectName(Guid congressId, Guid memberId, string fileName)
        {
            return string.Join(
                '/',
                "backoffice",
                "congresses",
                congressId.ToString("D"),
                "board-members",
                memberId.ToString("D"),
                fileName);
        }

        private static string NormalizeContentType(string? contentType)
            => string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();

        private static string? GetField(IDictionary<string, string?> fields, string key)
            => fields.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
                ? value.Trim()
                : null;

        private static string? Normalize(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static bool IsDeleted(object entity)
            => LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate") is not null;
    }
}

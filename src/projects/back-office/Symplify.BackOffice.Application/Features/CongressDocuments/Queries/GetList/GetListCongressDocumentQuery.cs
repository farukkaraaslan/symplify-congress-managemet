using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressDocuments.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Application.Features.CongressDocuments.Queries.GetList;

public class GetListCongressDocumentQuery
    : IRequest<GetListResponse<GetListCongressDocumentListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();

    public Guid CongressId { get; set; }

    public Guid? LanguageId { get; set; }

    public string? Culture { get; set; }

    public bool? IsActive { get; set; }

    public string? SearchText { get; set; }

    public string SortColumn { get; set; } = "order";

    public string SortDirection { get; set; } = "asc";

    public string[] Roles => new[]
    {
        CongressDocumentsOperationClaims.Admin,
        CongressDocumentsOperationClaims.Read
    };

    public bool BypassCache { get; }

    public string CacheKey => $"GetListCongressDocuments({PageRequest.Page},{PageRequest.PageSize},{CongressId},{LanguageId},{Culture},{IsActive},{SearchText},{SortColumn},{SortDirection})";

    public string CacheGroupKey => "GetCongressDocuments";

    public TimeSpan? SlidingExpiration { get; }

    public class GetListCongressDocumentQueryHandler
        : IRequestHandler<GetListCongressDocumentQuery, GetListResponse<GetListCongressDocumentListItemDto>>
    {
        private readonly ICongressDocumentRepository _repository;
        private readonly IDocumentTypeTranslationRepository _documentTypeTranslationRepository;
        private readonly ICongressDocumentTranslationRepository _documentTranslationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public GetListCongressDocumentQueryHandler(
            ICongressDocumentRepository repository,
            IDocumentTypeTranslationRepository documentTypeTranslationRepository,
            ICongressDocumentTranslationRepository documentTranslationRepository,
            IApplicationLanguageProvider languageProvider,
            ICurrentLanguageProvider currentLanguageProvider,
            ITranslationFallbackResolver fallbackResolver)
        {
            _repository = repository;
            _documentTypeTranslationRepository = documentTypeTranslationRepository;
            _documentTranslationRepository = documentTranslationRepository;
            _languageProvider = languageProvider;
            _currentLanguageProvider = currentLanguageProvider;
            _fallbackResolver = fallbackResolver;
        }

        public async Task<GetListResponse<GetListCongressDocumentListItemDto>> Handle(
            GetListCongressDocumentQuery request,
            CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(
                request.LanguageId,
                request.Culture,
                defaultLanguage,
                cancellationToken);

            List<CongressDocument> roots = _repository
                .Query()
                .ToList()
                .Where(entity =>
                    entity.CongressId == request.CongressId &&
                    !IsDeleted(entity))
                .ToList();

            if (request.IsActive.HasValue)
                roots = roots.Where(entity => entity.IsActive == request.IsActive.Value).ToList();

            HashSet<Guid> documentTypeIds = roots
                .Where(entity => entity.DocumentTypeId.HasValue)
                .Select(entity => entity.DocumentTypeId!.Value)
                .ToHashSet();

            List<DocumentTypeTranslation> documentTypeTranslations = documentTypeIds.Count == 0
                ? new List<DocumentTypeTranslation>()
                : _documentTypeTranslationRepository
                    .Query()
                    .ToList()
                    .Where(translation =>
                        documentTypeIds.Contains(translation.DocumentTypeId) &&
                        !IsDeleted(translation))
                    .ToList();

            HashSet<Guid> documentIds = roots
                .Select(entity => entity.Id)
                .ToHashSet();

            List<CongressDocumentTranslation> documentTranslations = documentIds.Count == 0
                ? new List<CongressDocumentTranslation>()
                : _documentTranslationRepository
                    .Query()
                    .ToList()
                    .Where(translation =>
                        documentIds.Contains(translation.CongressDocumentId) &&
                        !IsDeleted(translation))
                    .ToList();

            List<GetListCongressDocumentListItemDto> projectedItems = roots
                .Select(entity => Project(
                    entity,
                    documentTypeTranslations,
                    documentTranslations,
                    requestedLanguage.Id,
                    defaultLanguage.Id))
                .ToList();

            string? searchText = NormalizeSearchText(request.SearchText);

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string normalizedSearchText = searchText.ToLowerInvariant();

                projectedItems = projectedItems
                    .Where(item =>
                        Contains(item.OriginalFileName, normalizedSearchText) ||
                        Contains(item.DocumentTypeName, normalizedSearchText) ||
                        Contains(item.Description, normalizedSearchText) ||
                        Contains(item.ContentType, normalizedSearchText))
                    .ToList();
            }

            projectedItems = ApplyOrdering(
                projectedItems,
                request.SortColumn,
                request.SortDirection);

            int page = request.PageRequest.Page < 0 ? 0 : request.PageRequest.Page;
            int pageSize = request.PageRequest.PageSize <= 0 ? 20 : request.PageRequest.PageSize;
            int total = projectedItems.Count;
            int pages = (int)Math.Ceiling(total / (double)pageSize);

            List<GetListCongressDocumentListItemDto> items = projectedItems
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToList();

            return new GetListResponse<GetListCongressDocumentListItemDto>
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

        private GetListCongressDocumentListItemDto Project(
            CongressDocument entity,
            IEnumerable<DocumentTypeTranslation> documentTypeTranslations,
            IEnumerable<CongressDocumentTranslation> documentTranslations,
            Guid requestedLanguageId,
            Guid defaultLanguageId)
        {
            List<DocumentTypeTranslation> translations = entity.DocumentTypeId.HasValue
                ? documentTypeTranslations
                    .Where(translation => translation.DocumentTypeId == entity.DocumentTypeId.Value)
                    .ToList()
                : new List<DocumentTypeTranslation>();

            DocumentTypeTranslation? requestedTranslation = translations
                .FirstOrDefault(translation => translation.LanguageId == requestedLanguageId);

            DocumentTypeTranslation? displayTranslation = _fallbackResolver.Resolve(
                translations,
                requestedLanguageId,
                defaultLanguageId);

            List<CongressDocumentTranslation> documentDescriptionTranslations = documentTranslations
                .Where(translation => translation.CongressDocumentId == entity.Id)
                .ToList();

            CongressDocumentTranslation? documentDisplayTranslation = _fallbackResolver.Resolve(
                documentDescriptionTranslations,
                requestedLanguageId,
                defaultLanguageId);

            return new GetListCongressDocumentListItemDto
            {
                Id = entity.Id,
                CongressId = entity.CongressId,
                DocumentTypeId = entity.DocumentTypeId,
                DocumentTypeName = displayTranslation is null
                    ? null
                    : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Name"),
                Description = documentDisplayTranslation is null
                    ? null
                    : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(documentDisplayTranslation, "Description"),
                OriginalFileName = entity.OriginalFileName,
                BucketName = entity.BucketName,
                ObjectName = entity.ObjectName,
                ContentType = entity.ContentType,
                FileExtension = entity.FileExtension,
                FileSize = entity.FileSize,
                CoverImagePath = entity.CoverImagePath,
                CoverImageStorageProvider = entity.CoverImageStorageProvider,
                CoverImageBucketName = entity.CoverImageBucketName,
                CoverImageObjectName = entity.CoverImageObjectName,
                CoverImageFileName = entity.CoverImageFileName,
                CoverImageContentType = entity.CoverImageContentType,
                CoverImageFileSize = entity.CoverImageFileSize,
                CoverImageETag = entity.CoverImageETag,
                Order = entity.Order,
                IsActive = entity.IsActive,
                DisplayLanguageId = displayTranslation?.LanguageId ?? default,
                IsFallback = requestedTranslation is null && displayTranslation is not null
            };
        }

        private async Task<ApplicationLanguageDto> ResolveRequestedLanguageAsync(
            Guid? languageId,
            string? culture,
            ApplicationLanguageDto defaultLanguage,
            CancellationToken cancellationToken)
        {
            if (languageId.HasValue)
                return await _languageProvider.GetByIdAsync(languageId.Value, cancellationToken) ?? defaultLanguage;

            if (!string.IsNullOrWhiteSpace(culture))
                return await _languageProvider.GetByCultureAsync(culture, cancellationToken) ?? defaultLanguage;

            return await _currentLanguageProvider.GetCurrentLanguageAsync(cancellationToken);
        }

        private static List<GetListCongressDocumentListItemDto> ApplyOrdering(
            List<GetListCongressDocumentListItemDto> items,
            string? sortColumn,
            string? sortDirection)
        {
            bool descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            string normalizedSortColumn = string.IsNullOrWhiteSpace(sortColumn)
                ? "order"
                : sortColumn.Trim().ToLowerInvariant();

            IOrderedEnumerable<GetListCongressDocumentListItemDto> ordered = normalizedSortColumn switch
            {
                "originalfilename" or "file" or "filename" => descending
                    ? items.OrderByDescending(item => item.OriginalFileName ?? string.Empty)
                    : items.OrderBy(item => item.OriginalFileName ?? string.Empty),

                "documenttypename" or "documenttype" => descending
                    ? items.OrderByDescending(item => item.DocumentTypeName ?? string.Empty)
                    : items.OrderBy(item => item.DocumentTypeName ?? string.Empty),

                "filesize" => descending
                    ? items.OrderByDescending(item => item.FileSize ?? 0)
                    : items.OrderBy(item => item.FileSize ?? 0),

                "isactive" => descending
                    ? items.OrderByDescending(item => item.IsActive)
                    : items.OrderBy(item => item.IsActive),

                _ => descending
                    ? items.OrderByDescending(item => item.Order <= 0 ? int.MinValue : item.Order)
                    : items.OrderBy(item => item.Order <= 0 ? int.MaxValue : item.Order)
            };

            return ordered
                .ThenBy(item => item.Id)
                .ToList();
        }

        private static bool Contains(string? value, string normalizedSearchText)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.ToLowerInvariant().Contains(normalizedSearchText);
        }

        private static string? NormalizeSearchText(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static bool IsDeleted(object entity)
        {
            return LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate") is not null;
        }
    }
}

using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Queries.GetList;

public class GetListCongressSubmissionTypeQuery
    : IRequest<GetListResponse<GetListCongressSubmissionTypeListItemDto>>, ISecuredRequest, ICachableRequest
{
    public Guid CongressId { get; set; }
    public PageRequest PageRequest { get; set; } = new();
    public Guid? LanguageId { get; set; }
    public string? Culture { get; set; }
    public bool? IsActive { get; set; }
    public string? SearchText { get; set; }
    public string SortColumn { get; set; } = "order";
    public string SortDirection { get; set; } = "asc";

    public string[] Roles => new[] { CongressSubmissionTypesOperationClaims.Admin, CongressSubmissionTypesOperationClaims.Read };
    public bool BypassCache { get; }
    public string CacheKey => $"GetListCongressSubmissionTypes({PageRequest.Page},{PageRequest.PageSize},{CongressId},{LanguageId},{Culture},{IsActive},{SearchText},{SortColumn},{SortDirection})";
    public string CacheGroupKey => "GetCongressSubmissionTypes";
    public TimeSpan? SlidingExpiration { get; }

    public class GetListCongressSubmissionTypeQueryHandler
        : IRequestHandler<GetListCongressSubmissionTypeQuery, GetListResponse<GetListCongressSubmissionTypeListItemDto>>
    {
        private readonly ICongressSubmissionTypeRepository _repository;
        private readonly ISubmissionTypeRepository _submissionTypeRepository;
        private readonly ISubmissionTypeTranslationRepository _submissionTypeTranslationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public GetListCongressSubmissionTypeQueryHandler(
            ICongressSubmissionTypeRepository repository,
            ISubmissionTypeRepository submissionTypeRepository,
            ISubmissionTypeTranslationRepository submissionTypeTranslationRepository,
            IApplicationLanguageProvider languageProvider,
            ICurrentLanguageProvider currentLanguageProvider,
            ITranslationFallbackResolver fallbackResolver)
        {
            _repository = repository;
            _submissionTypeRepository = submissionTypeRepository;
            _submissionTypeTranslationRepository = submissionTypeTranslationRepository;
            _languageProvider = languageProvider;
            _currentLanguageProvider = currentLanguageProvider;
            _fallbackResolver = fallbackResolver;
        }

        public async Task<GetListResponse<GetListCongressSubmissionTypeListItemDto>> Handle(
            GetListCongressSubmissionTypeQuery request,
            CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(
                request.LanguageId,
                request.Culture,
                defaultLanguage,
                cancellationToken);

            List<CongressSubmissionType> relations = _repository
                .Query()
                .ToList()
                .Where(entity =>
                    entity.CongressId == request.CongressId &&
                    !IsDeleted(entity))
                .ToList();

            if (request.IsActive.HasValue)
                relations = relations.Where(entity => entity.IsActive == request.IsActive.Value).ToList();

            HashSet<Guid> submissionTypeIds = relations.Select(entity => entity.SubmissionTypeId).ToHashSet();

            List<SubmissionType> submissionTypes = submissionTypeIds.Count == 0
                ? new List<SubmissionType>()
                : _submissionTypeRepository
                    .Query()
                    .ToList()
                    .Where(submissionType => submissionTypeIds.Contains(submissionType.Id) && !IsDeleted(submissionType))
                    .ToList();

            List<SubmissionTypeTranslation> translations = submissionTypeIds.Count == 0
                ? new List<SubmissionTypeTranslation>()
                : _submissionTypeTranslationRepository
                    .Query()
                    .ToList()
                    .Where(translation => submissionTypeIds.Contains(translation.SubmissionTypeId) && !IsDeleted(translation))
                    .ToList();

            Dictionary<Guid, SubmissionType> submissionTypeById = submissionTypes.ToDictionary(submissionType => submissionType.Id);

            List<GetListCongressSubmissionTypeListItemDto> projectedItems = relations
                .Where(relation => submissionTypeById.ContainsKey(relation.SubmissionTypeId))
                .Select(relation => Project(
                    relation,
                    submissionTypeById[relation.SubmissionTypeId],
                    translations,
                    requestedLanguage.Id,
                    defaultLanguage.Id))
                .ToList();

            string? searchText = NormalizeSearchText(request.SearchText);

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string normalizedSearchText = searchText.ToLowerInvariant();

                projectedItems = projectedItems
                    .Where(item =>
                        Contains(item.Name, normalizedSearchText) ||
                        Contains(item.Description, normalizedSearchText) ||
                        Contains(item.Code, normalizedSearchText))
                    .ToList();
            }

            projectedItems = ApplyOrdering(projectedItems, request.SortColumn, request.SortDirection);

            int page = request.PageRequest.Page < 0 ? 0 : request.PageRequest.Page;
            int pageSize = request.PageRequest.PageSize <= 0 ? 20 : request.PageRequest.PageSize;
            int total = projectedItems.Count;
            int pages = pageSize <= 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);

            List<GetListCongressSubmissionTypeListItemDto> items = projectedItems
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToList();

            return new GetListResponse<GetListCongressSubmissionTypeListItemDto>
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

        private GetListCongressSubmissionTypeListItemDto Project(
            CongressSubmissionType relation,
            SubmissionType submissionType,
            IEnumerable<SubmissionTypeTranslation> translations,
            Guid requestedLanguageId,
            Guid defaultLanguageId)
        {
            List<SubmissionTypeTranslation> submissionTypeTranslations = translations
                .Where(translation => translation.SubmissionTypeId == submissionType.Id)
                .ToList();

            SubmissionTypeTranslation? requestedTranslation = submissionTypeTranslations
                .FirstOrDefault(translation => translation.LanguageId == requestedLanguageId);

            SubmissionTypeTranslation? displayTranslation = _fallbackResolver.Resolve(
                submissionTypeTranslations,
                requestedLanguageId,
                defaultLanguageId);

            return new GetListCongressSubmissionTypeListItemDto
            {
                Id = relation.Id,
                CongressId = relation.CongressId,
                SubmissionTypeId = relation.SubmissionTypeId,
                Code = submissionType.Code,
                Name = displayTranslation is null
                    ? string.Empty
                    : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Name") ?? string.Empty,
                Description = displayTranslation is null
                    ? null
                    : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Description"),
                Order = relation.Order,
                IsActive = relation.IsActive,
                SubmissionTypeIsActive = submissionType.IsActive,
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

        private static List<GetListCongressSubmissionTypeListItemDto> ApplyOrdering(
            List<GetListCongressSubmissionTypeListItemDto> items,
            string? sortColumn,
            string? sortDirection)
        {
            bool descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            string normalizedSortColumn = string.IsNullOrWhiteSpace(sortColumn)
                ? "order"
                : sortColumn.Trim().ToLowerInvariant();

            IOrderedEnumerable<GetListCongressSubmissionTypeListItemDto> ordered = normalizedSortColumn switch
            {
                "name" => descending
                    ? items.OrderByDescending(item => item.Name ?? string.Empty)
                    : items.OrderBy(item => item.Name ?? string.Empty),
                "code" => descending
                    ? items.OrderByDescending(item => item.Code ?? string.Empty)
                    : items.OrderBy(item => item.Code ?? string.Empty),
                "isactive" => descending
                    ? items.OrderByDescending(item => item.IsActive)
                    : items.OrderBy(item => item.IsActive),
                _ => descending
                    ? items.OrderByDescending(item => item.Order <= 0 ? int.MinValue : item.Order)
                    : items.OrderBy(item => item.Order <= 0 ? int.MaxValue : item.Order)
            };

            return ordered.ThenBy(item => item.Id).ToList();
        }

        private static bool Contains(string? value, string normalizedSearchText)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.ToLowerInvariant().Contains(normalizedSearchText);
        }

        private static string? NormalizeSearchText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static bool IsDeleted(object entity)
        {
            object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate");
            return deletedDate is not null;
        }
    }
}

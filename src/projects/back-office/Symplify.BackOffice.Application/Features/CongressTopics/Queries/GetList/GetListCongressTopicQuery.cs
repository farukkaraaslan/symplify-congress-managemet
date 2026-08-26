using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressTopics.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Application.Features.CongressTopics.Queries.GetList;

public class GetListCongressTopicQuery
    : IRequest<GetListResponse<GetListCongressTopicListItemDto>>, ISecuredRequest, ICachableRequest
{
    public Guid CongressId { get; set; }
    public PageRequest PageRequest { get; set; } = new();
    public Guid? LanguageId { get; set; }
    public string? Culture { get; set; }
    public bool? IsActive { get; set; }
    public string? SearchText { get; set; }
    public string SortColumn { get; set; } = "order";
    public string SortDirection { get; set; } = "asc";

    public string[] Roles => new[] { CongressTopicsOperationClaims.Admin, CongressTopicsOperationClaims.Read };
    public bool BypassCache { get; }
    public string CacheKey => $"GetListCongressTopics({PageRequest.Page},{PageRequest.PageSize},{CongressId},{LanguageId},{Culture},{IsActive},{SearchText},{SortColumn},{SortDirection})";
    public string CacheGroupKey => "GetCongressTopics";
    public TimeSpan? SlidingExpiration { get; }

    public class GetListCongressTopicQueryHandler
        : IRequestHandler<GetListCongressTopicQuery, GetListResponse<GetListCongressTopicListItemDto>>
    {
        private readonly ICongressTopicRepository _repository;
        private readonly ITopicRepository _topicRepository;
        private readonly ITopicTranslationRepository _topicTranslationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public GetListCongressTopicQueryHandler(
            ICongressTopicRepository repository,
            ITopicRepository topicRepository,
            ITopicTranslationRepository topicTranslationRepository,
            IApplicationLanguageProvider languageProvider,
            ICurrentLanguageProvider currentLanguageProvider,
            ITranslationFallbackResolver fallbackResolver)
        {
            _repository = repository;
            _topicRepository = topicRepository;
            _topicTranslationRepository = topicTranslationRepository;
            _languageProvider = languageProvider;
            _currentLanguageProvider = currentLanguageProvider;
            _fallbackResolver = fallbackResolver;
        }

        public async Task<GetListResponse<GetListCongressTopicListItemDto>> Handle(
            GetListCongressTopicQuery request,
            CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(
                request.LanguageId,
                request.Culture,
                defaultLanguage,
                cancellationToken);

            List<CongressTopic> relations = _repository
                .Query()
                .ToList()
                .Where(entity =>
                    entity.CongressId == request.CongressId &&
                    !IsDeleted(entity))
                .ToList();

            if (request.IsActive.HasValue)
                relations = relations.Where(entity => entity.IsActive == request.IsActive.Value).ToList();

            HashSet<Guid> topicIds = relations.Select(entity => entity.TopicId).ToHashSet();

            List<Topic> topics = topicIds.Count == 0
                ? new List<Topic>()
                : _topicRepository
                    .Query()
                    .ToList()
                    .Where(topic => topicIds.Contains(topic.Id) && !IsDeleted(topic))
                    .ToList();

            List<TopicTranslation> translations = topicIds.Count == 0
                ? new List<TopicTranslation>()
                : _topicTranslationRepository
                    .Query()
                    .ToList()
                    .Where(translation => topicIds.Contains(translation.TopicId) && !IsDeleted(translation))
                    .ToList();

            Dictionary<Guid, Topic> topicById = topics.ToDictionary(topic => topic.Id);

            List<GetListCongressTopicListItemDto> projectedItems = relations
                .Where(relation => topicById.ContainsKey(relation.TopicId))
                .Select(relation => Project(
                    relation,
                    topicById[relation.TopicId],
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

            List<GetListCongressTopicListItemDto> items = projectedItems
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToList();

            return new GetListResponse<GetListCongressTopicListItemDto>
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

        private GetListCongressTopicListItemDto Project(
            CongressTopic relation,
            Topic topic,
            IEnumerable<TopicTranslation> translations,
            Guid requestedLanguageId,
            Guid defaultLanguageId)
        {
            List<TopicTranslation> topicTranslations = translations
                .Where(translation => translation.TopicId == topic.Id)
                .ToList();

            TopicTranslation? requestedTranslation = topicTranslations
                .FirstOrDefault(translation => translation.LanguageId == requestedLanguageId);

            TopicTranslation? displayTranslation = _fallbackResolver.Resolve(
                topicTranslations,
                requestedLanguageId,
                defaultLanguageId);

            return new GetListCongressTopicListItemDto
            {
                Id = relation.Id,
                CongressId = relation.CongressId,
                TopicId = relation.TopicId,
                CategoryId = relation.CategoryId,
                Code = topic.Code,
                Name = displayTranslation is null
                    ? string.Empty
                    : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Name") ?? string.Empty,
                Description = displayTranslation is null
                    ? null
                    : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Description"),
                Order = relation.Order,
                IsActive = relation.IsActive,
                TopicIsActive = topic.IsActive,
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

        private static List<GetListCongressTopicListItemDto> ApplyOrdering(
            List<GetListCongressTopicListItemDto> items,
            string? sortColumn,
            string? sortDirection)
        {
            bool descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            string normalizedSortColumn = string.IsNullOrWhiteSpace(sortColumn)
                ? "order"
                : sortColumn.Trim().ToLowerInvariant();

            IOrderedEnumerable<GetListCongressTopicListItemDto> ordered = normalizedSortColumn switch
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

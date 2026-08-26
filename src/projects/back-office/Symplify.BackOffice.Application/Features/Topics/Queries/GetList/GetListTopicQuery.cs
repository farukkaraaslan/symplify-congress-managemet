using System.Linq.Expressions;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using Core.Persistence.Paging;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.Topics.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Application.Features.Topics.Queries.GetList;

public class GetListTopicQuery : IRequest<GetListResponse<GetListTopicListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();

    public Guid? LanguageId { get; set; }

    public string? Culture { get; set; }

    public bool? IsActive { get; set; }

    public string? SearchText { get; set; }

    public string SortColumn { get; set; } = "order";

    public string SortDirection { get; set; } = "asc";

    public string[] Roles => new[] { TopicsOperationClaims.Admin, TopicsOperationClaims.Read };

    public bool BypassCache { get; }

    public string CacheKey => $"GetListTopics({PageRequest.Page},{PageRequest.PageSize},{LanguageId},{Culture},{IsActive},{SearchText},{SortColumn},{SortDirection})";

    public string CacheGroupKey => "GetTopics";

    public TimeSpan? SlidingExpiration { get; }

    public class GetListTopicQueryHandler : IRequestHandler<GetListTopicQuery, GetListResponse<GetListTopicListItemDto>>
    {
        private readonly ITopicRepository _repository;
        private readonly ITopicTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public GetListTopicQueryHandler(
            ITopicRepository repository,
            ITopicTranslationRepository translationRepository,
            IApplicationLanguageProvider languageProvider,
            ICurrentLanguageProvider currentLanguageProvider,
            ITranslationFallbackResolver fallbackResolver)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _languageProvider = languageProvider;
            _currentLanguageProvider = currentLanguageProvider;
            _fallbackResolver = fallbackResolver;
        }

        public async Task<GetListResponse<GetListTopicListItemDto>> Handle(
            GetListTopicQuery request,
            CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(
                request.LanguageId,
                request.Culture,
                defaultLanguage,
                cancellationToken);

            IQueryable<TopicTranslation> translationQuery = _translationRepository.Query();

            IPaginate<Topic> roots = await _repository.GetListAsync(
                predicate: BuildPredicate(request, translationQuery),
                orderBy: BuildOrderBy(
                    request.SortColumn,
                    request.SortDirection,
                    requestedLanguage.Id,
                    defaultLanguage.Id,
                    translationQuery),
                index: request.PageRequest.Page < 0 ? 0 : request.PageRequest.Page,
                size: request.PageRequest.PageSize <= 0 ? 20 : request.PageRequest.PageSize,
                cancellationToken: cancellationToken);

            HashSet<Guid> ids = roots.Items.Select(entity => entity.Id).ToHashSet();

            List<TopicTranslation> translations = ids.Count == 0
                ? new List<TopicTranslation>()
                : translationQuery.Where(translation => ids.Contains(translation.TopicId)).ToList();

            List<GetListTopicListItemDto> items = roots.Items.Select(entity =>
            {
                List<TopicTranslation> rootTranslations = translations
                    .Where(translation => EqualityComparer<Guid>.Default.Equals(translation.TopicId, entity.Id))
                    .ToList();

                TopicTranslation? requestedTranslation = rootTranslations.FirstOrDefault(translation => translation.LanguageId == requestedLanguage.Id);
                TopicTranslation? displayTranslation = _fallbackResolver.Resolve(rootTranslations, requestedLanguage.Id, defaultLanguage.Id);

                return new GetListTopicListItemDto
                {
                    Id = entity.Id,
                    Code = entity.Code,
                    Order = entity.Order,
                    IsActive = entity.IsActive,
                    Name = displayTranslation is null ? string.Empty : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Name") ?? string.Empty,
                    Description = displayTranslation is null ? null : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Description"),
                    DisplayLanguageId = displayTranslation?.LanguageId ?? default,
                    IsFallback = requestedTranslation is null && displayTranslation is not null
                };
            }).ToList();

            return new GetListResponse<GetListTopicListItemDto>
            {
                Index = roots.Index,
                Size = roots.Size,
                Count = roots.Count,
                Pages = roots.Pages,
                HasPrevious = roots.HasPrevious,
                HasNext = roots.HasNext,
                Items = items
            };
        }

        private static Expression<Func<Topic, bool>>? BuildPredicate(
            GetListTopicQuery request,
            IQueryable<TopicTranslation> translationQuery)
        {
            bool hasIsActiveFilter = request.IsActive.HasValue;
            bool isActive = request.IsActive.GetValueOrDefault();
            string? searchText = NormalizeSearchText(request.SearchText);

            if (string.IsNullOrWhiteSpace(searchText))
            {
                return hasIsActiveFilter
                    ? entity => entity.IsActive == isActive
                    : null;
            }

            string normalizedSearchText = searchText.ToLower();

            IQueryable<Guid> matchingTranslationRootIds = translationQuery
                .Where(translation =>
                    (translation.Name != null && translation.Name.ToLower().Contains(normalizedSearchText)) ||
                    (translation.Description != null && translation.Description.ToLower().Contains(normalizedSearchText)))
                .Select(translation => translation.TopicId);

            if (hasIsActiveFilter)
            {
                return entity =>
                    entity.IsActive == isActive &&
                    (matchingTranslationRootIds.Contains(entity.Id) ||
                     (entity.Code != null && entity.Code.ToLower().Contains(normalizedSearchText)));
            }

            return entity =>
                matchingTranslationRootIds.Contains(entity.Id) ||
                (entity.Code != null && entity.Code.ToLower().Contains(normalizedSearchText));
        }

        private static Func<IQueryable<Topic>, IOrderedQueryable<Topic>> BuildOrderBy(
            string? sortColumn,
            string? sortDirection,
            Guid requestedLanguageId,
            Guid defaultLanguageId,
            IQueryable<TopicTranslation> translationQuery)
        {
            bool descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            string normalizedSortColumn = string.IsNullOrWhiteSpace(sortColumn)
                ? "order"
                : sortColumn.Trim().ToLowerInvariant();

            return normalizedSortColumn switch
            {
                "name" => query => descending
                    ? query
                        .OrderByDescending(entity =>
                            translationQuery
                                .Where(translation => translation.TopicId == entity.Id && translation.LanguageId == requestedLanguageId)
                                .Select(translation => translation.Name)
                                .FirstOrDefault()
                            ?? translationQuery
                                .Where(translation => translation.TopicId == entity.Id && translation.LanguageId == defaultLanguageId)
                                .Select(translation => translation.Name)
                                .FirstOrDefault()
                            ?? string.Empty)
                        .ThenBy(entity => entity.Order)
                        .ThenBy(entity => entity.Id)
                    : query
                        .OrderBy(entity =>
                            translationQuery
                                .Where(translation => translation.TopicId == entity.Id && translation.LanguageId == requestedLanguageId)
                                .Select(translation => translation.Name)
                                .FirstOrDefault()
                            ?? translationQuery
                                .Where(translation => translation.TopicId == entity.Id && translation.LanguageId == defaultLanguageId)
                                .Select(translation => translation.Name)
                                .FirstOrDefault()
                            ?? string.Empty)
                        .ThenBy(entity => entity.Order)
                        .ThenBy(entity => entity.Id),

                "description" => query => descending
                    ? query
                        .OrderByDescending(entity =>
                            translationQuery
                                .Where(translation => translation.TopicId == entity.Id && translation.LanguageId == requestedLanguageId)
                                .Select(translation => translation.Description)
                                .FirstOrDefault()
                            ?? translationQuery
                                .Where(translation => translation.TopicId == entity.Id && translation.LanguageId == defaultLanguageId)
                                .Select(translation => translation.Description)
                                .FirstOrDefault()
                            ?? string.Empty)
                        .ThenBy(entity => entity.Order)
                        .ThenBy(entity => entity.Id)
                    : query
                        .OrderBy(entity =>
                            translationQuery
                                .Where(translation => translation.TopicId == entity.Id && translation.LanguageId == requestedLanguageId)
                                .Select(translation => translation.Description)
                                .FirstOrDefault()
                            ?? translationQuery
                                .Where(translation => translation.TopicId == entity.Id && translation.LanguageId == defaultLanguageId)
                                .Select(translation => translation.Description)
                                .FirstOrDefault()
                            ?? string.Empty)
                        .ThenBy(entity => entity.Order)
                        .ThenBy(entity => entity.Id),

                "isactive" => query => descending
                    ? query.OrderByDescending(entity => entity.IsActive).ThenBy(entity => entity.Order).ThenBy(entity => entity.Id)
                    : query.OrderBy(entity => entity.IsActive).ThenBy(entity => entity.Order).ThenBy(entity => entity.Id),

                _ => query => descending
                    ? query.OrderByDescending(entity => entity.Order).ThenBy(entity => entity.Id)
                    : query.OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order).ThenBy(entity => entity.Id)
            };
        }

        private static string? NormalizeSearchText(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
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
    }
}

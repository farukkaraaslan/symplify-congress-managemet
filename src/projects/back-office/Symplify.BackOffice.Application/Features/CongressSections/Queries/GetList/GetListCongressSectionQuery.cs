using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressSections.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressSections.Queries.GetList;

public class GetListCongressSectionQuery : IRequest<GetListResponse<GetListCongressSectionListItemDto>>, ISecuredRequest, ICachableRequest
{
    public Guid CongressId { get; set; }

    public PageRequest PageRequest { get; set; } = new();

    public Guid? LanguageId { get; set; }

    public string? Culture { get; set; }

    public bool? IsActive { get; set; }

    public string? SearchText { get; set; }

    public string SortColumn { get; set; } = "order";

    public string SortDirection { get; set; } = "asc";

    public string[] Roles => new[] { CongressSectionsOperationClaims.Admin, CongressSectionsOperationClaims.Read };

    public bool BypassCache { get; }

    public string CacheKey => $"GetListCongressSections({CongressId},{PageRequest.Page},{PageRequest.PageSize},{LanguageId},{Culture},{IsActive},{SearchText},{SortColumn},{SortDirection})";

    public string CacheGroupKey => "GetCongressSections";

    public TimeSpan? SlidingExpiration { get; }

    public class GetListCongressSectionQueryHandler : IRequestHandler<GetListCongressSectionQuery, GetListResponse<GetListCongressSectionListItemDto>>
    {
        private readonly ICongressSectionRepository _repository;
        private readonly ICongressSectionTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public GetListCongressSectionQueryHandler(
            ICongressSectionRepository repository,
            ICongressSectionTranslationRepository translationRepository,
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

        public async Task<GetListResponse<GetListCongressSectionListItemDto>> Handle(
            GetListCongressSectionQuery request,
            CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(
                request.LanguageId,
                request.Culture,
                defaultLanguage,
                cancellationToken);

            List<CongressSection> roots = _repository
                .Query()
                .ToList()
                .Where(entity => request.CongressId == Guid.Empty || entity.CongressId == request.CongressId)
                .Where(entity => !IsDeleted(entity))
                .ToList();

            if (request.IsActive.HasValue)
            {
                roots = roots
                    .Where(entity => entity.IsActive == request.IsActive.Value)
                    .ToList();
            }

            HashSet<Guid> rootIds = roots.Select(entity => entity.Id).ToHashSet();

            List<CongressSectionTranslation> translations = _translationRepository
                .Query()
                .ToList()
                .Where(translation => rootIds.Contains(translation.CongressSectionId))
                .ToList();

            List<GetListCongressSectionListItemDto> projectedItems = roots.Select(entity =>
            {
                List<CongressSectionTranslation> rootTranslations = translations
                    .Where(translation => translation.CongressSectionId == entity.Id)
                    .ToList();

                CongressSectionTranslation? requestedTranslation = rootTranslations
                    .FirstOrDefault(translation => translation.LanguageId == requestedLanguage.Id);

                CongressSectionTranslation? displayTranslation = _fallbackResolver.Resolve(
                    rootTranslations,
                    requestedLanguage.Id,
                    defaultLanguage.Id);

                return new GetListCongressSectionListItemDto
                {
                    Id = entity.Id,
                    CongressId = entity.CongressId,
                    BindingKey = entity.BindingKey,
                    Order = entity.Order,
                    IsActive = entity.IsActive,
                    Title = displayTranslation is null
                        ? string.Empty
                        : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Title") ?? string.Empty,
                    Content = displayTranslation is null
                        ? null
                        : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Content"),
                    DisplayLanguageId = displayTranslation?.LanguageId ?? default,
                    IsFallback = requestedTranslation is null && displayTranslation is not null
                };
            }).ToList();

            projectedItems = ApplySearch(projectedItems, request.SearchText);
            projectedItems = ApplySort(projectedItems, request.SortColumn, request.SortDirection);

            int page = request.PageRequest.Page < 0 ? 0 : request.PageRequest.Page;
            int pageSize = request.PageRequest.PageSize <= 0 ? 20 : request.PageRequest.PageSize;
            int total = projectedItems.Count;

            List<GetListCongressSectionListItemDto> pagedItems = projectedItems
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToList();

            int pages = (int)Math.Ceiling(total / (double)pageSize);

            return new GetListResponse<GetListCongressSectionListItemDto>
            {
                Index = page,
                Size = pageSize,
                Count = total,
                Pages = pages,
                HasPrevious = page > 0,
                HasNext = page + 1 < pages,
                Items = pagedItems
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

        private static List<GetListCongressSectionListItemDto> ApplySearch(
            List<GetListCongressSectionListItemDto> items,
            string? searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return items;

            string normalizedSearchText = searchText.Trim();

            return items
                .Where(item =>
                    Contains(item.BindingKey, normalizedSearchText) ||
                    Contains(item.Title, normalizedSearchText) ||
                    Contains(StripHtml(item.Content), normalizedSearchText))
                .ToList();
        }

        private static List<GetListCongressSectionListItemDto> ApplySort(
            List<GetListCongressSectionListItemDto> items,
            string? sortColumn,
            string? sortDirection)
        {
            bool descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            string normalizedSortColumn = string.IsNullOrWhiteSpace(sortColumn) ? "order" : sortColumn.Trim();

            IOrderedEnumerable<GetListCongressSectionListItemDto> ordered = normalizedSortColumn.ToLowerInvariant() switch
            {
                "bindingkey" => descending
                    ? items.OrderByDescending(item => item.BindingKey)
                    : items.OrderBy(item => item.BindingKey),

                "title" => descending
                    ? items.OrderByDescending(item => item.Title)
                    : items.OrderBy(item => item.Title),

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

        private static bool Contains(string? source, string searchText)
        {
            return !string.IsNullOrWhiteSpace(source) &&
                   source.Contains(searchText, StringComparison.OrdinalIgnoreCase);
        }

        private static string StripHtml(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            bool insideTag = false;
            Span<char> buffer = value.Length <= 4096
                ? stackalloc char[value.Length]
                : new char[value.Length];

            int bufferIndex = 0;

            foreach (char character in value)
            {
                if (character == '<')
                {
                    insideTag = true;
                    continue;
                }

                if (character == '>')
                {
                    insideTag = false;
                    continue;
                }

                if (!insideTag)
                    buffer[bufferIndex++] = character;
            }

            return new string(buffer[..bufferIndex]);
        }

        private static bool IsDeleted(object entity)
        {
            object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate");
            return deletedDate is not null;
        }
    }
}

using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressImportantDates.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressImportantDates.Queries.GetList;

public class GetListCongressImportantDateQuery : IRequest<GetListResponse<GetListCongressImportantDateListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public Guid CongressId { get; set; }
    public Guid? LanguageId { get; set; }
    public string? Culture { get; set; }
    public bool? IsActive { get; set; }
    public string? SearchText { get; set; }
    public string SortColumn { get; set; } = "order";
    public string SortDirection { get; set; } = "asc";

    public string[] Roles => new[] { CongressImportantDatesOperationClaims.Admin, CongressImportantDatesOperationClaims.Read };
    public bool BypassCache { get; }
    public string CacheKey => $"GetListCongressImportantDates({CongressId},{PageRequest.Page},{PageRequest.PageSize},{LanguageId},{Culture},{IsActive},{SearchText},{SortColumn},{SortDirection})";
    public string CacheGroupKey => "GetCongressImportantDates";
    public TimeSpan? SlidingExpiration { get; }

    public class GetListCongressImportantDateQueryHandler : IRequestHandler<GetListCongressImportantDateQuery, GetListResponse<GetListCongressImportantDateListItemDto>>
    {
        private readonly ICongressImportantDateRepository _repository;
        private readonly ICongressImportantDateTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public GetListCongressImportantDateQueryHandler(
            ICongressImportantDateRepository repository,
            ICongressImportantDateTranslationRepository translationRepository,
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

        public async Task<GetListResponse<GetListCongressImportantDateListItemDto>> Handle(
            GetListCongressImportantDateQuery request,
            CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(
                request.LanguageId,
                request.Culture,
                defaultLanguage,
                cancellationToken);

            List<CongressImportantDate> roots = _repository.Query()
                .ToList()
                .Where(entity =>
                    request.CongressId == Guid.Empty || entity.CongressId == request.CongressId)
                .Where(entity => !IsDeleted(entity))
                .ToList();

            if (request.IsActive.HasValue)
                roots = roots.Where(entity => entity.IsActive == request.IsActive.Value).ToList();

            List<CongressImportantDateTranslation> allTranslations = _translationRepository.Query()
                .ToList()
                .Where(translation => roots.Select(root => root.Id).Contains(translation.CongressImportantDateId))
                .Where(translation => !IsDeleted(translation))
                .ToList();

            string? searchText = NormalizeSearchText(request.SearchText);

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string normalizedSearchText = searchText.ToLowerInvariant();
                HashSet<Guid> matchingIds = allTranslations
                    .Where(translation =>
                        (translation.Title != null && translation.Title.ToLower().Contains(normalizedSearchText)) ||
                        (translation.Description != null && translation.Description.ToLower().Contains(normalizedSearchText)))
                    .Select(translation => translation.CongressImportantDateId)
                    .ToHashSet();

                roots = roots.Where(entity => matchingIds.Contains(entity.Id)).ToList();
            }

            roots = ApplyOrdering(roots, request.SortColumn, request.SortDirection, allTranslations, requestedLanguage.Id, defaultLanguage.Id);

            int page = request.PageRequest.Page < 0 ? 0 : request.PageRequest.Page;
            int pageSize = request.PageRequest.PageSize <= 0 ? 20 : request.PageRequest.PageSize;
            int total = roots.Count;

            List<CongressImportantDate> paged = roots
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToList();

            HashSet<Guid> pagedIds = paged.Select(entity => entity.Id).ToHashSet();

            List<CongressImportantDateTranslation> translations = allTranslations
                .Where(translation => pagedIds.Contains(translation.CongressImportantDateId))
                .ToList();

            List<GetListCongressImportantDateListItemDto> items = paged.Select(entity =>
            {
                List<CongressImportantDateTranslation> rootTranslations = translations
                    .Where(translation => EqualityComparer<Guid>.Default.Equals(translation.CongressImportantDateId, entity.Id))
                    .ToList();

                CongressImportantDateTranslation? requestedTranslation = rootTranslations.FirstOrDefault(translation => translation.LanguageId == requestedLanguage.Id);
                CongressImportantDateTranslation? displayTranslation = _fallbackResolver.Resolve(rootTranslations, requestedLanguage.Id, defaultLanguage.Id);

                return new GetListCongressImportantDateListItemDto
                {
                    Id = entity.Id,
                    CongressId = entity.CongressId,
                    StartDate = entity.StartDate,
                    EndDate = entity.EndDate,
                    Order = entity.Order,
                    IsActive = entity.IsActive,
                    Title = displayTranslation is null ? string.Empty : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Title") ?? string.Empty,
                    Description = displayTranslation is null ? null : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Description"),
                    DisplayLanguageId = displayTranslation?.LanguageId ?? default,
                    IsFallback = requestedTranslation is null && displayTranslation is not null
                };
            }).ToList();

            int pages = (int)Math.Ceiling(total / (double)pageSize);

            return new GetListResponse<GetListCongressImportantDateListItemDto>
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

        private static List<CongressImportantDate> ApplyOrdering(
            List<CongressImportantDate> roots,
            string? sortColumn,
            string? sortDirection,
            List<CongressImportantDateTranslation> translations,
            Guid requestedLanguageId,
            Guid defaultLanguageId)
        {
            bool descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            string normalizedSortColumn = string.IsNullOrWhiteSpace(sortColumn)
                ? "order"
                : sortColumn.Trim().ToLowerInvariant();

            return normalizedSortColumn switch
            {
                
                "startdate" => descending
                    ? roots.OrderByDescending(entity => entity.StartDate).ThenByDescending(entity => entity.EndDate).ThenBy(entity => NormalizeOrder(entity.Order)).ThenBy(entity => entity.Id).ToList()
                    : roots.OrderBy(entity => entity.StartDate).ThenBy(entity => entity.EndDate).ThenBy(entity => NormalizeOrder(entity.Order)).ThenBy(entity => entity.Id).ToList(),

                "enddate" => descending
                    ? roots.OrderByDescending(entity => entity.EndDate).ThenByDescending(entity => entity.StartDate).ThenBy(entity => NormalizeOrder(entity.Order)).ThenBy(entity => entity.Id).ToList()
                    : roots.OrderBy(entity => entity.EndDate).ThenBy(entity => entity.StartDate).ThenBy(entity => NormalizeOrder(entity.Order)).ThenBy(entity => entity.Id).ToList(),

                "title" => descending
                    ? roots.OrderByDescending(entity => ResolveTitle(entity.Id, translations, requestedLanguageId, defaultLanguageId)).ThenBy(entity => NormalizeOrder(entity.Order)).ThenBy(entity => entity.Id).ToList()
                    : roots.OrderBy(entity => ResolveTitle(entity.Id, translations, requestedLanguageId, defaultLanguageId)).ThenBy(entity => NormalizeOrder(entity.Order)).ThenBy(entity => entity.Id).ToList(),

                "isactive" => descending
                    ? roots.OrderByDescending(entity => entity.IsActive).ThenBy(entity => NormalizeOrder(entity.Order)).ThenBy(entity => entity.Id).ToList()
                    : roots.OrderBy(entity => entity.IsActive).ThenBy(entity => NormalizeOrder(entity.Order)).ThenBy(entity => entity.Id).ToList(),

                _ => descending
                    ? roots.OrderByDescending(entity => entity.Order <= 0 ? int.MinValue : entity.Order).ThenBy(entity => entity.Id).ToList()
                    : roots.OrderBy(entity => NormalizeOrder(entity.Order)).ThenBy(entity => entity.Id).ToList()
            };
        }

        private static string ResolveTitle(
            Guid entityId,
            List<CongressImportantDateTranslation> translations,
            Guid requestedLanguageId,
            Guid defaultLanguageId)
        {
            CongressImportantDateTranslation? requested = translations
                .FirstOrDefault(translation => translation.CongressImportantDateId == entityId && translation.LanguageId == requestedLanguageId);

            if (!string.IsNullOrWhiteSpace(requested?.Title))
                return requested.Title;

            CongressImportantDateTranslation? fallback = translations
                .FirstOrDefault(translation => translation.CongressImportantDateId == entityId && translation.LanguageId == defaultLanguageId);

            return fallback?.Title ?? string.Empty;
        }

        private static int NormalizeOrder(int order)
        {
            return order <= 0 ? int.MaxValue : order;
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

using System.Linq.Expressions;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using Core.Persistence.Paging;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.TransactionStatusPhases.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.TransactionStatusPhases.Queries.GetList;

public class GetListTransactionStatusPhaseQuery
    : IRequest<GetListResponse<GetListTransactionStatusPhaseListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();

    public Guid? LanguageId { get; set; }

    public string? Culture { get; set; }

    public bool? IsActive { get; set; }

    public string? SearchText { get; set; }

    public string SortColumn { get; set; } = "order";

    public string SortDirection { get; set; } = "asc";

    public string[] Roles => new[] { TransactionStatusPhasesOperationClaims.Admin, TransactionStatusPhasesOperationClaims.Read };

    public bool BypassCache { get; }

    public string CacheKey => $"GetListTransactionStatusPhases({PageRequest.Page},{PageRequest.PageSize},{LanguageId},{Culture},{IsActive},{SearchText},{SortColumn},{SortDirection})";

    public string CacheGroupKey => "GetTransactionStatusPhases";

    public TimeSpan? SlidingExpiration { get; }

    public class GetListTransactionStatusPhaseQueryHandler
        : IRequestHandler<GetListTransactionStatusPhaseQuery, GetListResponse<GetListTransactionStatusPhaseListItemDto>>
    {
        private readonly ITransactionStatusPhaseRepository _repository;
        private readonly ITransactionStatusPhaseTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public GetListTransactionStatusPhaseQueryHandler(
            ITransactionStatusPhaseRepository repository,
            ITransactionStatusPhaseTranslationRepository translationRepository,
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

        public async Task<GetListResponse<GetListTransactionStatusPhaseListItemDto>> Handle(
            GetListTransactionStatusPhaseQuery request,
            CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(
                request.LanguageId,
                request.Culture,
                defaultLanguage,
                cancellationToken);

            IQueryable<TransactionStatusPhaseTranslation> translationQuery = _translationRepository.Query();

            IPaginate<TransactionStatusPhase> roots = await _repository.GetListAsync(
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

            HashSet<int> ids = roots.Items.Select(entity => entity.Id).ToHashSet();

            List<TransactionStatusPhaseTranslation> translations = ids.Count == 0
                ? new List<TransactionStatusPhaseTranslation>()
                : translationQuery.Where(translation => ids.Contains(translation.TransactionStatusPhaseId)).ToList();

            List<GetListTransactionStatusPhaseListItemDto> items = roots.Items.Select(entity =>
            {
                List<TransactionStatusPhaseTranslation> rootTranslations = translations
                    .Where(translation => translation.TransactionStatusPhaseId == entity.Id)
                    .ToList();

                TransactionStatusPhaseTranslation? requestedTranslation = rootTranslations
                    .FirstOrDefault(translation => translation.LanguageId == requestedLanguage.Id);

                TransactionStatusPhaseTranslation? displayTranslation = _fallbackResolver.Resolve(
                    rootTranslations,
                    requestedLanguage.Id,
                    defaultLanguage.Id);

                return new GetListTransactionStatusPhaseListItemDto
                {
                    Id = entity.Id,
                    Code = entity.Code,
                    Order = entity.Order,
                    IsActive = entity.IsActive,
                    Name = displayTranslation is null
                        ? string.Empty
                        : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Name") ?? string.Empty,
                    Description = displayTranslation is null
                        ? null
                        : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Description"),
                    DisplayLanguageId = displayTranslation?.LanguageId ?? default,
                    IsFallback = requestedTranslation is null && displayTranslation is not null
                };
            }).ToList();

            return new GetListResponse<GetListTransactionStatusPhaseListItemDto>
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

        private static Expression<Func<TransactionStatusPhase, bool>>? BuildPredicate(
            GetListTransactionStatusPhaseQuery request,
            IQueryable<TransactionStatusPhaseTranslation> translationQuery)
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

            IQueryable<int> matchingTranslationRootIds = translationQuery
                .Where(translation =>
                    (translation.Name != null && translation.Name.ToLower().Contains(normalizedSearchText)) ||
                    (translation.Description != null && translation.Description.ToLower().Contains(normalizedSearchText)))
                .Select(translation => translation.TransactionStatusPhaseId);

            if (hasIsActiveFilter)
            {
                return entity =>
                    entity.IsActive == isActive &&
                    (matchingTranslationRootIds.Contains(entity.Id) ||
                     entity.Code.ToLower().Contains(normalizedSearchText));
            }

            return entity =>
                matchingTranslationRootIds.Contains(entity.Id) ||
                entity.Code.ToLower().Contains(normalizedSearchText);
        }

        private static Func<IQueryable<TransactionStatusPhase>, IOrderedQueryable<TransactionStatusPhase>> BuildOrderBy(
            string? sortColumn,
            string? sortDirection,
            Guid requestedLanguageId,
            Guid defaultLanguageId,
            IQueryable<TransactionStatusPhaseTranslation> translationQuery)
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
                                .Where(translation => translation.TransactionStatusPhaseId == entity.Id && translation.LanguageId == requestedLanguageId)
                                .Select(translation => translation.Name)
                                .FirstOrDefault()
                            ?? translationQuery
                                .Where(translation => translation.TransactionStatusPhaseId == entity.Id && translation.LanguageId == defaultLanguageId)
                                .Select(translation => translation.Name)
                                .FirstOrDefault()
                            ?? string.Empty)
                        .ThenBy(entity => entity.Order)
                        .ThenBy(entity => entity.Id)
                    : query
                        .OrderBy(entity =>
                            translationQuery
                                .Where(translation => translation.TransactionStatusPhaseId == entity.Id && translation.LanguageId == requestedLanguageId)
                                .Select(translation => translation.Name)
                                .FirstOrDefault()
                            ?? translationQuery
                                .Where(translation => translation.TransactionStatusPhaseId == entity.Id && translation.LanguageId == defaultLanguageId)
                                .Select(translation => translation.Name)
                                .FirstOrDefault()
                            ?? string.Empty)
                        .ThenBy(entity => entity.Order)
                        .ThenBy(entity => entity.Id),

                "code" => query => descending
                    ? query.OrderByDescending(entity => entity.Code).ThenBy(entity => entity.Order).ThenBy(entity => entity.Id)
                    : query.OrderBy(entity => entity.Code).ThenBy(entity => entity.Order).ThenBy(entity => entity.Id),

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

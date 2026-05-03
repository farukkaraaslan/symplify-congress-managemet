using System.Linq.Expressions;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using Core.Persistence.Paging;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Queries.GetList;

public class GetListTransactionStatusTransitionQuery
    : IRequest<GetListResponse<GetListTransactionStatusTransitionListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();

    public Guid? LanguageId { get; set; }

    public string? Culture { get; set; }

    public bool? IsActive { get; set; }

    public int? FromStatusId { get; set; }

    public int? ToStatusId { get; set; }

    public string? SearchText { get; set; }

    public string SortColumn { get; set; } = "fromStatus";

    public string SortDirection { get; set; } = "asc";

    public string[] Roles => new[] { TransactionStatusTransitionsOperationClaims.Admin, TransactionStatusTransitionsOperationClaims.Read };

    public bool BypassCache { get; }

    public string CacheKey => $"GetListTransactionStatusTransitions({PageRequest.Page},{PageRequest.PageSize},{LanguageId},{Culture},{IsActive},{FromStatusId},{ToStatusId},{SearchText},{SortColumn},{SortDirection})";

    public string CacheGroupKey => "GetTransactionStatusTransitions";

    public TimeSpan? SlidingExpiration { get; }

    public class GetListTransactionStatusTransitionQueryHandler
        : IRequestHandler<GetListTransactionStatusTransitionQuery, GetListResponse<GetListTransactionStatusTransitionListItemDto>>
    {
        private readonly ITransactionStatusTransitionRepository _repository;
        private readonly ITransactionStatusTransitionTranslationRepository _translationRepository;
        private readonly ITransactionStatusRepository _transactionStatusRepository;
        private readonly ITransactionStatusTranslationRepository _transactionStatusTranslationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public GetListTransactionStatusTransitionQueryHandler(
            ITransactionStatusTransitionRepository repository,
            ITransactionStatusTransitionTranslationRepository translationRepository,
            ITransactionStatusRepository transactionStatusRepository,
            ITransactionStatusTranslationRepository transactionStatusTranslationRepository,
            IApplicationLanguageProvider languageProvider,
            ICurrentLanguageProvider currentLanguageProvider,
            ITranslationFallbackResolver fallbackResolver)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _transactionStatusRepository = transactionStatusRepository;
            _transactionStatusTranslationRepository = transactionStatusTranslationRepository;
            _languageProvider = languageProvider;
            _currentLanguageProvider = currentLanguageProvider;
            _fallbackResolver = fallbackResolver;
        }

        public async Task<GetListResponse<GetListTransactionStatusTransitionListItemDto>> Handle(
            GetListTransactionStatusTransitionQuery request,
            CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(
                request.LanguageId,
                request.Culture,
                defaultLanguage,
                cancellationToken);

            IQueryable<TransactionStatusTransitionTranslation> translationQuery = _translationRepository.Query();
            IQueryable<TransactionStatusTranslation> statusTranslationQuery = _transactionStatusTranslationRepository.Query();
            IQueryable<TransactionStatus> statusQuery = _transactionStatusRepository.Query();

            IPaginate<TransactionStatusTransition> roots = await _repository.GetListAsync(
                predicate: BuildPredicate(request, translationQuery, statusTranslationQuery, statusQuery),
                orderBy: BuildOrderBy(
                    request.SortColumn,
                    request.SortDirection,
                    requestedLanguage.Id,
                    defaultLanguage.Id,
                    translationQuery,
                    statusTranslationQuery,
                    statusQuery),
                index: request.PageRequest.Page < 0 ? 0 : request.PageRequest.Page,
                size: request.PageRequest.PageSize <= 0 ? 20 : request.PageRequest.PageSize,
                cancellationToken: cancellationToken);

            HashSet<int> transitionIds = roots.Items.Select(entity => entity.Id).ToHashSet();
            HashSet<int> statusIds = roots.Items
                .SelectMany(entity => new[] { entity.FromStatusId, entity.ToStatusId })
                .ToHashSet();

            List<TransactionStatusTransitionTranslation> translations = transitionIds.Count == 0
                ? new List<TransactionStatusTransitionTranslation>()
                : translationQuery.Where(translation => transitionIds.Contains(translation.TransactionStatusTransitionId)).ToList();

            List<TransactionStatus> statuses = statusIds.Count == 0
                ? new List<TransactionStatus>()
                : statusQuery.Where(status => statusIds.Contains(status.Id)).ToList();

            List<TransactionStatusTranslation> statusTranslations = statusIds.Count == 0
                ? new List<TransactionStatusTranslation>()
                : statusTranslationQuery.Where(translation => statusIds.Contains(translation.TransactionStatusId)).ToList();

            List<GetListTransactionStatusTransitionListItemDto> items = roots.Items.Select(entity =>
            {
                List<TransactionStatusTransitionTranslation> rootTranslations = translations
                    .Where(translation => translation.TransactionStatusTransitionId == entity.Id)
                    .ToList();

                TransactionStatusTransitionTranslation? requestedTranslation = rootTranslations
                    .FirstOrDefault(translation => translation.LanguageId == requestedLanguage.Id);

                TransactionStatusTransitionTranslation? displayTranslation = _fallbackResolver.Resolve(
                    rootTranslations,
                    requestedLanguage.Id,
                    defaultLanguage.Id);

                TransactionStatus? fromStatus = statuses.FirstOrDefault(status => status.Id == entity.FromStatusId);
                TransactionStatus? toStatus = statuses.FirstOrDefault(status => status.Id == entity.ToStatusId);

                return new GetListTransactionStatusTransitionListItemDto
                {
                    Id = entity.Id,
                    FromStatusId = entity.FromStatusId,
                    FromStatusCode = fromStatus?.Code ?? string.Empty,
                    FromStatusName = ResolveStatusName(statusTranslations, entity.FromStatusId, requestedLanguage.Id, defaultLanguage.Id),
                    ToStatusId = entity.ToStatusId,
                    ToStatusCode = toStatus?.Code ?? string.Empty,
                    ToStatusName = ResolveStatusName(statusTranslations, entity.ToStatusId, requestedLanguage.Id, defaultLanguage.Id),
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

            return new GetListResponse<GetListTransactionStatusTransitionListItemDto>
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

        private static Expression<Func<TransactionStatusTransition, bool>>? BuildPredicate(
            GetListTransactionStatusTransitionQuery request,
            IQueryable<TransactionStatusTransitionTranslation> translationQuery,
            IQueryable<TransactionStatusTranslation> statusTranslationQuery,
            IQueryable<TransactionStatus> statusQuery)
        {
            bool hasIsActiveFilter = request.IsActive.HasValue;
            bool isActive = request.IsActive.GetValueOrDefault();
            bool hasFromStatusFilter = request.FromStatusId.HasValue;
            int fromStatusId = request.FromStatusId.GetValueOrDefault();
            bool hasToStatusFilter = request.ToStatusId.HasValue;
            int toStatusId = request.ToStatusId.GetValueOrDefault();
            string? searchText = NormalizeSearchText(request.SearchText);

            if (string.IsNullOrWhiteSpace(searchText))
            {
                if (!hasIsActiveFilter && !hasFromStatusFilter && !hasToStatusFilter)
                    return null;

                return transition =>
                    (!hasIsActiveFilter || transition.IsActive == isActive) &&
                    (!hasFromStatusFilter || transition.FromStatusId == fromStatusId) &&
                    (!hasToStatusFilter || transition.ToStatusId == toStatusId);
            }

            string normalizedSearchText = searchText.ToLower();

            IQueryable<int> matchingTransitionIds = translationQuery
                .Where(translation =>
                    (translation.Name != null && translation.Name.ToLower().Contains(normalizedSearchText)) ||
                    (translation.Description != null && translation.Description.ToLower().Contains(normalizedSearchText)))
                .Select(translation => translation.TransactionStatusTransitionId);

            IQueryable<int> matchingStatusIdsByTranslation = statusTranslationQuery
                .Where(translation =>
                    (translation.Name != null && translation.Name.ToLower().Contains(normalizedSearchText)) ||
                    (translation.Description != null && translation.Description.ToLower().Contains(normalizedSearchText)))
                .Select(translation => translation.TransactionStatusId);

            IQueryable<int> matchingStatusIdsByCode = statusQuery
                .Where(status => status.Code.ToLower().Contains(normalizedSearchText))
                .Select(status => status.Id);

            return transition =>
                (!hasIsActiveFilter || transition.IsActive == isActive) &&
                (!hasFromStatusFilter || transition.FromStatusId == fromStatusId) &&
                (!hasToStatusFilter || transition.ToStatusId == toStatusId) &&
                (matchingTransitionIds.Contains(transition.Id) ||
                 matchingStatusIdsByTranslation.Contains(transition.FromStatusId) ||
                 matchingStatusIdsByTranslation.Contains(transition.ToStatusId) ||
                 matchingStatusIdsByCode.Contains(transition.FromStatusId) ||
                 matchingStatusIdsByCode.Contains(transition.ToStatusId));
        }

        private static Func<IQueryable<TransactionStatusTransition>, IOrderedQueryable<TransactionStatusTransition>> BuildOrderBy(
            string? sortColumn,
            string? sortDirection,
            Guid requestedLanguageId,
            Guid defaultLanguageId,
            IQueryable<TransactionStatusTransitionTranslation> translationQuery,
            IQueryable<TransactionStatusTranslation> statusTranslationQuery,
            IQueryable<TransactionStatus> statusQuery)
        {
            bool descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            string normalizedSortColumn = string.IsNullOrWhiteSpace(sortColumn)
                ? "fromstatus"
                : sortColumn.Trim().ToLowerInvariant();

            return normalizedSortColumn switch
            {
                "name" => query => descending
                    ? query
                        .OrderByDescending(entity =>
                            translationQuery
                                .Where(translation => translation.TransactionStatusTransitionId == entity.Id && translation.LanguageId == requestedLanguageId)
                                .Select(translation => translation.Name)
                                .FirstOrDefault()
                            ?? translationQuery
                                .Where(translation => translation.TransactionStatusTransitionId == entity.Id && translation.LanguageId == defaultLanguageId)
                                .Select(translation => translation.Name)
                                .FirstOrDefault()
                            ?? string.Empty)
                        .ThenBy(entity => entity.Id)
                    : query
                        .OrderBy(entity =>
                            translationQuery
                                .Where(translation => translation.TransactionStatusTransitionId == entity.Id && translation.LanguageId == requestedLanguageId)
                                .Select(translation => translation.Name)
                                .FirstOrDefault()
                            ?? translationQuery
                                .Where(translation => translation.TransactionStatusTransitionId == entity.Id && translation.LanguageId == defaultLanguageId)
                                .Select(translation => translation.Name)
                                .FirstOrDefault()
                            ?? string.Empty)
                        .ThenBy(entity => entity.Id),

                "tostatus" => query => descending
                    ? query.OrderByDescending(entity => entity.ToStatusId).ThenBy(entity => entity.FromStatusId).ThenBy(entity => entity.Id)
                    : query.OrderBy(entity => entity.ToStatusId).ThenBy(entity => entity.FromStatusId).ThenBy(entity => entity.Id),

                "isactive" => query => descending
                    ? query.OrderByDescending(entity => entity.IsActive).ThenBy(entity => entity.FromStatusId).ThenBy(entity => entity.ToStatusId).ThenBy(entity => entity.Id)
                    : query.OrderBy(entity => entity.IsActive).ThenBy(entity => entity.FromStatusId).ThenBy(entity => entity.ToStatusId).ThenBy(entity => entity.Id),

                _ => query => descending
                    ? query.OrderByDescending(entity => entity.FromStatusId).ThenBy(entity => entity.ToStatusId).ThenBy(entity => entity.Id)
                    : query.OrderBy(entity => entity.FromStatusId).ThenBy(entity => entity.ToStatusId).ThenBy(entity => entity.Id)
            };
        }

        private static string ResolveStatusName(
            IEnumerable<TransactionStatusTranslation> translations,
            int statusId,
            Guid requestedLanguageId,
            Guid defaultLanguageId)
        {
            TransactionStatusTranslation? translation = translations
                .Where(item => item.TransactionStatusId == statusId)
                .FirstOrDefault(item => item.LanguageId == requestedLanguageId)
                ?? translations
                    .Where(item => item.TransactionStatusId == statusId)
                    .FirstOrDefault(item => item.LanguageId == defaultLanguageId);

            if (translation is null)
                return string.Empty;

            return (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(translation, "Name") ?? string.Empty;
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

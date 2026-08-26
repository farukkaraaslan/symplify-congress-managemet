using System.Linq.Expressions;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using Core.Persistence.Paging;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.TransactionStatuses.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.TransactionStatuses.Queries.GetList;

public class GetListTransactionStatusQuery
    : IRequest<GetListResponse<GetListTransactionStatusListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();

    public Guid? LanguageId { get; set; }

    public string? Culture { get; set; }

    public int? TransactionStatusPhaseId { get; set; }

    public bool? IsActive { get; set; }

    public string? SearchText { get; set; }

    public string SortColumn { get; set; } = "Phase";

    public string SortDirection { get; set; } = "asc";

    public string[] Roles => new[] { TransactionStatusesOperationClaims.Admin, TransactionStatusesOperationClaims.Read };

    public bool BypassCache { get; }

    public string CacheKey => $"GetListTransactionStatuses({PageRequest.Page},{PageRequest.PageSize},{LanguageId},{Culture},{TransactionStatusPhaseId},{IsActive},{SearchText},{SortColumn},{SortDirection})";

    public string CacheGroupKey => "GetTransactionStatuses";

    public TimeSpan? SlidingExpiration { get; }

    public class GetListTransactionStatusQueryHandler
        : IRequestHandler<GetListTransactionStatusQuery, GetListResponse<GetListTransactionStatusListItemDto>>
    {
        private readonly ITransactionStatusRepository _repository;
        private readonly ITransactionStatusTranslationRepository _translationRepository;
        private readonly ITransactionStatusPhaseRepository _phaseRepository;
        private readonly ITransactionStatusPhaseTranslationRepository _phaseTranslationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public GetListTransactionStatusQueryHandler(
            ITransactionStatusRepository repository,
            ITransactionStatusTranslationRepository translationRepository,
            ITransactionStatusPhaseRepository phaseRepository,
            ITransactionStatusPhaseTranslationRepository phaseTranslationRepository,
            IApplicationLanguageProvider languageProvider,
            ICurrentLanguageProvider currentLanguageProvider,
            ITranslationFallbackResolver fallbackResolver)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _phaseRepository = phaseRepository;
            _phaseTranslationRepository = phaseTranslationRepository;
            _languageProvider = languageProvider;
            _currentLanguageProvider = currentLanguageProvider;
            _fallbackResolver = fallbackResolver;
        }

        public async Task<GetListResponse<GetListTransactionStatusListItemDto>> Handle(
            GetListTransactionStatusQuery request,
            CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(
                request.LanguageId,
                request.Culture,
                defaultLanguage,
                cancellationToken);

            IQueryable<TransactionStatusTranslation> translationQuery = _translationRepository.Query();
            IQueryable<TransactionStatusPhase> phaseQuery = _phaseRepository.Query();
            IQueryable<TransactionStatusPhaseTranslation> phaseTranslationQuery = _phaseTranslationRepository.Query();

            IPaginate<TransactionStatus> roots = await _repository.GetListAsync(
                predicate: BuildPredicate(request, translationQuery, phaseTranslationQuery, phaseQuery),
                orderBy: BuildOrderBy(
                    request.SortColumn,
                    request.SortDirection,
                    requestedLanguage.Id,
                    defaultLanguage.Id,
                    translationQuery,
                    phaseTranslationQuery,
                    phaseQuery),
                index: request.PageRequest.Page < 0 ? 0 : request.PageRequest.Page,
                size: request.PageRequest.PageSize <= 0 ? 20 : request.PageRequest.PageSize,
                cancellationToken: cancellationToken);

            HashSet<int> ids = roots.Items.Select(entity => entity.Id).ToHashSet();
            HashSet<int> phaseIds = roots.Items.Select(entity => entity.TransactionStatusPhaseId).ToHashSet();

            List<TransactionStatusTranslation> translations = ids.Count == 0
                ? new List<TransactionStatusTranslation>()
                : translationQuery.Where(translation => ids.Contains(translation.TransactionStatusId)).ToList();

            List<TransactionStatusPhase> phases = phaseIds.Count == 0
                ? new List<TransactionStatusPhase>()
                : phaseQuery.Where(phase => phaseIds.Contains(phase.Id)).ToList();

            List<TransactionStatusPhaseTranslation> phaseTranslations = phaseIds.Count == 0
                ? new List<TransactionStatusPhaseTranslation>()
                : phaseTranslationQuery.Where(translation => phaseIds.Contains(translation.TransactionStatusPhaseId)).ToList();

            List<GetListTransactionStatusListItemDto> items = roots.Items.Select(entity =>
            {
                List<TransactionStatusTranslation> rootTranslations = translations
                    .Where(translation => translation.TransactionStatusId == entity.Id)
                    .ToList();

                TransactionStatusTranslation? requestedTranslation = rootTranslations
                    .FirstOrDefault(translation => translation.LanguageId == requestedLanguage.Id);

                TransactionStatusTranslation? displayTranslation = _fallbackResolver.Resolve(
                    rootTranslations,
                    requestedLanguage.Id,
                    defaultLanguage.Id);

                TransactionStatusPhase? phase = phases.FirstOrDefault(item => item.Id == entity.TransactionStatusPhaseId);
                List<TransactionStatusPhaseTranslation> rootPhaseTranslations = phaseTranslations
                    .Where(translation => translation.TransactionStatusPhaseId == entity.TransactionStatusPhaseId)
                    .ToList();

                TransactionStatusPhaseTranslation? displayPhaseTranslation = _fallbackResolver.Resolve(
                    rootPhaseTranslations,
                    requestedLanguage.Id,
                    defaultLanguage.Id);

                return new GetListTransactionStatusListItemDto
                {
                    Id = entity.Id,
                    TransactionStatusPhaseId = entity.TransactionStatusPhaseId,
                    TransactionStatusPhaseCode = phase?.Code ?? string.Empty,
                    TransactionStatusPhaseName = displayPhaseTranslation is null
                        ? string.Empty
                        : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayPhaseTranslation, "Name") ?? string.Empty,
                    Code = entity.Code,
                    Order = entity.Order,
                    IsEditable = entity.IsEditable,
                    IsFinal = entity.IsFinal,
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

            return new GetListResponse<GetListTransactionStatusListItemDto>
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

        private static Expression<Func<TransactionStatus, bool>>? BuildPredicate(
            GetListTransactionStatusQuery request,
            IQueryable<TransactionStatusTranslation> translationQuery,
            IQueryable<TransactionStatusPhaseTranslation> phaseTranslationQuery,
            IQueryable<TransactionStatusPhase> phaseQuery)
        {
            bool hasIsActiveFilter = request.IsActive.HasValue;
            bool isActive = request.IsActive.GetValueOrDefault();
            bool hasPhaseFilter = request.TransactionStatusPhaseId.HasValue;
            int phaseId = request.TransactionStatusPhaseId.GetValueOrDefault();
            string? searchText = NormalizeSearchText(request.SearchText);

            if (string.IsNullOrWhiteSpace(searchText))
            {
                if (!hasIsActiveFilter && !hasPhaseFilter)
                    return null;

                return entity =>
                    (!hasIsActiveFilter || entity.IsActive == isActive) &&
                    (!hasPhaseFilter || entity.TransactionStatusPhaseId == phaseId);
            }

            string normalizedSearchText = searchText.ToLower();

            IQueryable<int> matchingTranslationRootIds = translationQuery
                .Where(translation =>
                    (translation.Name != null && translation.Name.ToLower().Contains(normalizedSearchText)) ||
                    (translation.Description != null && translation.Description.ToLower().Contains(normalizedSearchText)))
                .Select(translation => translation.TransactionStatusId);

            IQueryable<int> matchingPhaseIdsByTranslation = phaseTranslationQuery
                .Where(translation =>
                    (translation.Name != null && translation.Name.ToLower().Contains(normalizedSearchText)) ||
                    (translation.Description != null && translation.Description.ToLower().Contains(normalizedSearchText)))
                .Select(translation => translation.TransactionStatusPhaseId);

            IQueryable<int> matchingPhaseIdsByCode = phaseQuery
                .Where(phase => phase.Code.ToLower().Contains(normalizedSearchText))
                .Select(phase => phase.Id);

            return entity =>
                (!hasIsActiveFilter || entity.IsActive == isActive) &&
                (!hasPhaseFilter || entity.TransactionStatusPhaseId == phaseId) &&
                (matchingTranslationRootIds.Contains(entity.Id) ||
                 matchingPhaseIdsByTranslation.Contains(entity.TransactionStatusPhaseId) ||
                 matchingPhaseIdsByCode.Contains(entity.TransactionStatusPhaseId) ||
                 entity.Code.ToLower().Contains(normalizedSearchText));
        }

        private static Func<IQueryable<TransactionStatus>, IOrderedQueryable<TransactionStatus>> BuildOrderBy(
            string? sortColumn,
            string? sortDirection,
            Guid requestedLanguageId,
            Guid defaultLanguageId,
            IQueryable<TransactionStatusTranslation> translationQuery,
            IQueryable<TransactionStatusPhaseTranslation> phaseTranslationQuery,
            IQueryable<TransactionStatusPhase> phaseQuery)
        {
            bool descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            string normalizedSortColumn = string.IsNullOrWhiteSpace(sortColumn)
                ? "phase"
                : sortColumn.Trim().ToLowerInvariant();

            return normalizedSortColumn switch
            {
                "phase" or "transactionstatusphase" or "transactionstatusphaseid" => query => descending
                    ? query
                        .OrderByDescending(entity => phaseQuery
                            .Where(phase => phase.Id == entity.TransactionStatusPhaseId)
                            .Select(phase => phase.Order)
                            .FirstOrDefault())
                        .ThenByDescending(entity => entity.Order)
                        .ThenBy(entity => entity.Id)
                    : query
                        .OrderBy(entity => phaseQuery
                            .Where(phase => phase.Id == entity.TransactionStatusPhaseId)
                            .Select(phase => phase.Order)
                            .FirstOrDefault())
                        .ThenBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                        .ThenBy(entity => entity.Id),

                "phasename" => query => descending
                    ? query
                        .OrderByDescending(entity =>
                            phaseTranslationQuery
                                .Where(translation => translation.TransactionStatusPhaseId == entity.TransactionStatusPhaseId && translation.LanguageId == requestedLanguageId)
                                .Select(translation => translation.Name)
                                .FirstOrDefault()
                            ?? phaseTranslationQuery
                                .Where(translation => translation.TransactionStatusPhaseId == entity.TransactionStatusPhaseId && translation.LanguageId == defaultLanguageId)
                                .Select(translation => translation.Name)
                                .FirstOrDefault()
                            ?? string.Empty)
                        .ThenBy(entity => entity.Order)
                        .ThenBy(entity => entity.Id)
                    : query
                        .OrderBy(entity =>
                            phaseTranslationQuery
                                .Where(translation => translation.TransactionStatusPhaseId == entity.TransactionStatusPhaseId && translation.LanguageId == requestedLanguageId)
                                .Select(translation => translation.Name)
                                .FirstOrDefault()
                            ?? phaseTranslationQuery
                                .Where(translation => translation.TransactionStatusPhaseId == entity.TransactionStatusPhaseId && translation.LanguageId == defaultLanguageId)
                                .Select(translation => translation.Name)
                                .FirstOrDefault()
                            ?? string.Empty)
                        .ThenBy(entity => entity.Order)
                        .ThenBy(entity => entity.Id),

                "name" => query => descending
                    ? query
                        .OrderByDescending(entity =>
                            translationQuery
                                .Where(translation => translation.TransactionStatusId == entity.Id && translation.LanguageId == requestedLanguageId)
                                .Select(translation => translation.Name)
                                .FirstOrDefault()
                            ?? translationQuery
                                .Where(translation => translation.TransactionStatusId == entity.Id && translation.LanguageId == defaultLanguageId)
                                .Select(translation => translation.Name)
                                .FirstOrDefault()
                            ?? string.Empty)
                        .ThenBy(entity => entity.Order)
                        .ThenBy(entity => entity.Id)
                    : query
                        .OrderBy(entity =>
                            translationQuery
                                .Where(translation => translation.TransactionStatusId == entity.Id && translation.LanguageId == requestedLanguageId)
                                .Select(translation => translation.Name)
                                .FirstOrDefault()
                            ?? translationQuery
                                .Where(translation => translation.TransactionStatusId == entity.Id && translation.LanguageId == defaultLanguageId)
                                .Select(translation => translation.Name)
                                .FirstOrDefault()
                            ?? string.Empty)
                        .ThenBy(entity => entity.Order)
                        .ThenBy(entity => entity.Id),

                "code" => query => descending
                    ? query.OrderByDescending(entity => entity.Code).ThenBy(entity => entity.Order).ThenBy(entity => entity.Id)
                    : query.OrderBy(entity => entity.Code).ThenBy(entity => entity.Order).ThenBy(entity => entity.Id),

                "iseditable" => query => descending
                    ? query.OrderByDescending(entity => entity.IsEditable).ThenBy(entity => entity.Order).ThenBy(entity => entity.Id)
                    : query.OrderBy(entity => entity.IsEditable).ThenBy(entity => entity.Order).ThenBy(entity => entity.Id),

                "isfinal" => query => descending
                    ? query.OrderByDescending(entity => entity.IsFinal).ThenBy(entity => entity.Order).ThenBy(entity => entity.Id)
                    : query.OrderBy(entity => entity.IsFinal).ThenBy(entity => entity.Order).ThenBy(entity => entity.Id),

                "isactive" => query => descending
                    ? query.OrderByDescending(entity => entity.IsActive).ThenBy(entity => entity.Order).ThenBy(entity => entity.Id)
                    : query.OrderBy(entity => entity.IsActive).ThenBy(entity => entity.Order).ThenBy(entity => entity.Id),

                _ => query => descending
                    ? query
                        .OrderByDescending(entity => phaseQuery
                            .Where(phase => phase.Id == entity.TransactionStatusPhaseId)
                            .Select(phase => phase.Order)
                            .FirstOrDefault())
                        .ThenByDescending(entity => entity.Order)
                        .ThenBy(entity => entity.Id)
                    : query
                        .OrderBy(entity => phaseQuery
                            .Where(phase => phase.Id == entity.TransactionStatusPhaseId)
                            .Select(phase => phase.Order)
                            .FirstOrDefault())
                        .ThenBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                        .ThenBy(entity => entity.Id)
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

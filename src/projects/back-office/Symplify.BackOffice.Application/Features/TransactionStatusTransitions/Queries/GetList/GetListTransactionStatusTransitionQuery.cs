using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using MediatR;
using Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Constants;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;
using Symplify.BackOffice.Domain.Workflow;
namespace Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Queries.GetList;
public class GetListTransactionStatusTransitionQuery : IRequest<GetListResponse<GetListTransactionStatusTransitionListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public Guid? LanguageId { get; set; }
    public string? Culture { get; set; }
    public bool? IsActive { get; set; }
    public string[] Roles => new[] { TransactionStatusTransitionsOperationClaims.Admin, TransactionStatusTransitionsOperationClaims.Read };
    public bool BypassCache { get; }
    public string CacheKey => $"GetListTransactionStatusTransitions({PageRequest.Page},{PageRequest.PageSize},{LanguageId},{Culture},{IsActive})";
    public string CacheGroupKey => "GetTransactionStatusTransitions";
    public TimeSpan? SlidingExpiration { get; }
    public class GetListTransactionStatusTransitionQueryHandler : IRequestHandler<GetListTransactionStatusTransitionQuery, GetListResponse<GetListTransactionStatusTransitionListItemDto>>
    {
        private readonly ITransactionStatusTransitionRepository _repository; private readonly ITransactionStatusTransitionTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider; private readonly ICurrentLanguageProvider _currentLanguageProvider; private readonly ITranslationFallbackResolver _fallbackResolver;
        public GetListTransactionStatusTransitionQueryHandler(ITransactionStatusTransitionRepository repository, ITransactionStatusTransitionTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider, ICurrentLanguageProvider currentLanguageProvider, ITranslationFallbackResolver fallbackResolver) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; _currentLanguageProvider = currentLanguageProvider; _fallbackResolver = fallbackResolver; }
        public async Task<GetListResponse<GetListTransactionStatusTransitionListItemDto>> Handle(GetListTransactionStatusTransitionQuery request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(request.LanguageId, request.Culture, defaultLanguage, cancellationToken);
            List<TransactionStatusTransition> roots = _repository.Query().ToList();
            if (request.IsActive.HasValue) roots = roots.Where(x => x.GetType().GetProperty("IsActive")?.GetValue(x) is bool b && b == request.IsActive.Value).ToList();
            roots = roots.OrderBy(x => x.GetType().GetProperty("Order")?.GetValue(x) as int? ?? int.MaxValue).ThenBy(x => x.Id).ToList();
            int page = request.PageRequest.Page < 0 ? 0 : request.PageRequest.Page;
            int pageSize = request.PageRequest.PageSize <= 0 ? 20 : request.PageRequest.PageSize;
            int total = roots.Count;
            List<TransactionStatusTransition> paged = roots.Skip(page * pageSize).Take(pageSize).ToList();
            HashSet<int> ids = paged.Select(x => x.Id).ToHashSet();
            List<TransactionStatusTransitionTranslation> translations = _translationRepository.Query().ToList().Where(x => ids.Contains(x.TransactionStatusTransitionId)).ToList();
            List<GetListTransactionStatusTransitionListItemDto> items = paged.Select(entity =>
            {
                List<TransactionStatusTransitionTranslation> rootTranslations = translations.Where(x => EqualityComparer<int>.Default.Equals(x.TransactionStatusTransitionId, entity.Id)).ToList();
                TransactionStatusTransitionTranslation? requestedTranslation = rootTranslations.FirstOrDefault(x => x.LanguageId == requestedLanguage.Id);
                TransactionStatusTransitionTranslation? displayTranslation = _fallbackResolver.Resolve(rootTranslations, requestedLanguage.Id, defaultLanguage.Id);
                return new GetListTransactionStatusTransitionListItemDto
                {
                    Id = entity.Id,
                FromStatusId = entity.FromStatusId,
                ToStatusId = entity.ToStatusId,
                IsActive = entity.IsActive,
                Name = displayTranslation is null ? string.Empty : (string)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Name")!,
                Description = displayTranslation is null ? null : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Description")!,
                    DisplayLanguageId = displayTranslation?.LanguageId ?? default,
                    IsFallback = requestedTranslation is null && displayTranslation is not null
                };
            }).ToList();
            int pages = (int)Math.Ceiling(total / (double)pageSize);
            return new GetListResponse<GetListTransactionStatusTransitionListItemDto> { Index = page, Size = pageSize, Count = total, Pages = pages, HasPrevious = page > 0, HasNext = page + 1 < pages, Items = items };
        }
        private async Task<ApplicationLanguageDto> ResolveRequestedLanguageAsync(Guid? languageId, string? culture, ApplicationLanguageDto defaultLanguage, CancellationToken cancellationToken)
        {
            if (languageId.HasValue) return await _languageProvider.GetByIdAsync(languageId.Value, cancellationToken) ?? defaultLanguage;
            if (!string.IsNullOrWhiteSpace(culture)) return await _languageProvider.GetByCultureAsync(culture, cancellationToken) ?? defaultLanguage;
            return await _currentLanguageProvider.GetCurrentLanguageAsync(cancellationToken);
        }
    }
}

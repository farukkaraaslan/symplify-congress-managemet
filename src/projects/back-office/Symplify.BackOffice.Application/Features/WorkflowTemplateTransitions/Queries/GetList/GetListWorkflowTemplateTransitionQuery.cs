using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.WorkflowTemplateTransitions.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.WorkflowTemplateTransitions.Queries.GetList;

public class GetListWorkflowTemplateTransitionQuery : IRequest<IList<GetListWorkflowTemplateTransitionListItemDto>>, ISecuredRequest, ICachableRequest
{
    public Guid WorkflowTemplateId { get; set; }
    public Guid? LanguageId { get; set; }
    public string? Culture { get; set; }

    public string[] Roles => new[] { WorkflowTemplateTransitionsOperationClaims.Admin, WorkflowTemplateTransitionsOperationClaims.Read };
    public bool BypassCache { get; }
    public string CacheKey => $"GetWorkflowTemplateTransitions({WorkflowTemplateId},{LanguageId},{Culture})";
    public string CacheGroupKey => "GetWorkflowTemplates";
    public TimeSpan? SlidingExpiration { get; }

    public class GetListWorkflowTemplateTransitionQueryHandler : IRequestHandler<GetListWorkflowTemplateTransitionQuery, IList<GetListWorkflowTemplateTransitionListItemDto>>
    {
        private readonly IWorkflowTemplateTransitionRepository _repository;
        private readonly ITransactionStatusTransitionRepository _transitionRepository;
        private readonly ITransactionStatusTransitionTranslationRepository _transitionTranslationRepository;
        private readonly ITransactionStatusTranslationRepository _statusTranslationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public GetListWorkflowTemplateTransitionQueryHandler(
            IWorkflowTemplateTransitionRepository repository,
            ITransactionStatusTransitionRepository transitionRepository,
            ITransactionStatusTransitionTranslationRepository transitionTranslationRepository,
            ITransactionStatusTranslationRepository statusTranslationRepository,
            IApplicationLanguageProvider languageProvider,
            ICurrentLanguageProvider currentLanguageProvider,
            ITranslationFallbackResolver fallbackResolver)
        {
            _repository = repository;
            _transitionRepository = transitionRepository;
            _transitionTranslationRepository = transitionTranslationRepository;
            _statusTranslationRepository = statusTranslationRepository;
            _languageProvider = languageProvider;
            _currentLanguageProvider = currentLanguageProvider;
            _fallbackResolver = fallbackResolver;
        }

        public async Task<IList<GetListWorkflowTemplateTransitionListItemDto>> Handle(GetListWorkflowTemplateTransitionQuery request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = request.LanguageId.HasValue ? await _languageProvider.GetByIdAsync(request.LanguageId.Value, cancellationToken) ?? defaultLanguage : !string.IsNullOrWhiteSpace(request.Culture) ? await _languageProvider.GetByCultureAsync(request.Culture, cancellationToken) ?? defaultLanguage : await _currentLanguageProvider.GetCurrentLanguageAsync(cancellationToken);

            List<WorkflowTemplateTransition> items = _repository.Query().ToList().Where(entity => entity.WorkflowTemplateId == request.WorkflowTemplateId).OrderBy(entity => entity.Order).ThenBy(entity => entity.Id).ToList();
            HashSet<int> transitionIds = items.Select(entity => entity.TransactionStatusTransitionId).ToHashSet();
            List<TransactionStatusTransition> transitions = transitionIds.Count == 0 ? new() : _transitionRepository.Query().Where(entity => transitionIds.Contains(entity.Id)).ToList();
            List<TransactionStatusTransitionTranslation> transitionTranslations = transitionIds.Count == 0 ? new() : _transitionTranslationRepository.Query().Where(entity => transitionIds.Contains(entity.TransactionStatusTransitionId)).ToList();
            HashSet<int> statusIds = transitions.SelectMany(entity => new[] { entity.FromStatusId, entity.ToStatusId }).ToHashSet();
            List<TransactionStatusTranslation> statusTranslations = statusIds.Count == 0 ? new() : _statusTranslationRepository.Query().Where(entity => statusIds.Contains(entity.TransactionStatusId)).ToList();

            return items.Select(item =>
            {
                TransactionStatusTransition? transition = transitions.FirstOrDefault(entity => entity.Id == item.TransactionStatusTransitionId);
                TransactionStatusTransitionTranslation? displayTransition = _fallbackResolver.Resolve(transitionTranslations.Where(x => x.TransactionStatusTransitionId == item.TransactionStatusTransitionId).ToList(), requestedLanguage.Id, defaultLanguage.Id);
                TransactionStatusTranslation? fromStatus = transition is null ? null : _fallbackResolver.Resolve(statusTranslations.Where(x => x.TransactionStatusId == transition.FromStatusId).ToList(), requestedLanguage.Id, defaultLanguage.Id);
                TransactionStatusTranslation? toStatus = transition is null ? null : _fallbackResolver.Resolve(statusTranslations.Where(x => x.TransactionStatusId == transition.ToStatusId).ToList(), requestedLanguage.Id, defaultLanguage.Id);

                return new GetListWorkflowTemplateTransitionListItemDto
                {
                    Id = item.Id,
                    WorkflowTemplateId = item.WorkflowTemplateId,
                    TransactionStatusTransitionId = item.TransactionStatusTransitionId,
                    FromStatusName = fromStatus?.Name ?? string.Empty,
                    ToStatusName = toStatus?.Name ?? string.Empty,
                    TransitionName = displayTransition?.Name ?? string.Empty,
                    TransitionDescription = displayTransition?.Description,
                    Order = item.Order,
                    IsActive = item.IsActive
                };
            }).ToList();
        }
    }
}

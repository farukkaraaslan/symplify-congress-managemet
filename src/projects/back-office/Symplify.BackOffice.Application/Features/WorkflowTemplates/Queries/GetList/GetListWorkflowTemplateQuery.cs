using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using Core.Persistence.Paging;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.WorkflowTemplates.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.WorkflowTemplates.Queries.GetList;

public class GetListWorkflowTemplateQuery : IRequest<GetListResponse<GetListWorkflowTemplateListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public Guid? LanguageId { get; set; }
    public string? Culture { get; set; }
    public bool? IsActive { get; set; }
    public string? SearchText { get; set; }

    public string[] Roles => new[] { WorkflowTemplatesOperationClaims.Admin, WorkflowTemplatesOperationClaims.Read };
    public bool BypassCache { get; }
    public string CacheKey => $"GetListWorkflowTemplates({PageRequest.Page},{PageRequest.PageSize},{LanguageId},{Culture},{IsActive},{SearchText})";
    public string CacheGroupKey => "GetWorkflowTemplates";
    public TimeSpan? SlidingExpiration { get; }

    public class GetListWorkflowTemplateQueryHandler : IRequestHandler<GetListWorkflowTemplateQuery, GetListResponse<GetListWorkflowTemplateListItemDto>>
    {
        private readonly IWorkflowTemplateRepository _repository;
        private readonly IWorkflowTemplateTranslationRepository _translationRepository;
        private readonly ITransactionStatusTranslationRepository _statusTranslationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public GetListWorkflowTemplateQueryHandler(
            IWorkflowTemplateRepository repository,
            IWorkflowTemplateTranslationRepository translationRepository,
            ITransactionStatusTranslationRepository statusTranslationRepository,
            IApplicationLanguageProvider languageProvider,
            ICurrentLanguageProvider currentLanguageProvider,
            ITranslationFallbackResolver fallbackResolver)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _statusTranslationRepository = statusTranslationRepository;
            _languageProvider = languageProvider;
            _currentLanguageProvider = currentLanguageProvider;
            _fallbackResolver = fallbackResolver;
        }

        public async Task<GetListResponse<GetListWorkflowTemplateListItemDto>> Handle(GetListWorkflowTemplateQuery request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(request.LanguageId, request.Culture, defaultLanguage, cancellationToken);
            string? searchText = string.IsNullOrWhiteSpace(request.SearchText) ? null : request.SearchText.Trim().ToLowerInvariant();

            IQueryable<WorkflowTemplate> query = _repository.Query();

            if (request.IsActive.HasValue)
                query = query.Where(entity => entity.IsActive == request.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                IQueryable<Guid> matchingIds = _translationRepository.Query()
                    .Where(translation =>
                        translation.Name.ToLower().Contains(searchText) ||
                        (translation.Description != null && translation.Description.ToLower().Contains(searchText)))
                    .Select(translation => translation.WorkflowTemplateId);

                query = query.Where(entity => entity.Code.ToLower().Contains(searchText) || matchingIds.Contains(entity.Id));
            }

            IPaginate<WorkflowTemplate> roots = await _repository.GetListAsync(
                predicate: entity => query.Select(x => x.Id).Contains(entity.Id),
                orderBy: q => q.OrderByDescending(entity => entity.IsDefault).ThenBy(entity => entity.Code).ThenBy(entity => entity.Id),
                index: request.PageRequest.Page < 0 ? 0 : request.PageRequest.Page,
                size: request.PageRequest.PageSize <= 0 ? 20 : request.PageRequest.PageSize,
                cancellationToken: cancellationToken);

            HashSet<Guid> ids = roots.Items.Select(entity => entity.Id).ToHashSet();
            List<WorkflowTemplateTranslation> translations = ids.Count == 0 ? new() : _translationRepository.Query().Where(translation => ids.Contains(translation.WorkflowTemplateId)).ToList();
            HashSet<int> statusIds = roots.Items.Where(entity => entity.InitialTransactionStatusId.HasValue).Select(entity => entity.InitialTransactionStatusId!.Value).ToHashSet();
            List<TransactionStatusTranslation> statusTranslations = statusIds.Count == 0 ? new() : _statusTranslationRepository.Query().Where(translation => statusIds.Contains(translation.TransactionStatusId)).ToList();

            List<GetListWorkflowTemplateListItemDto> items = roots.Items.Select(entity =>
            {
                List<WorkflowTemplateTranslation> rootTranslations = translations.Where(translation => translation.WorkflowTemplateId == entity.Id).ToList();
                WorkflowTemplateTranslation? requestedTranslation = rootTranslations.FirstOrDefault(translation => translation.LanguageId == requestedLanguage.Id);
                WorkflowTemplateTranslation? displayTranslation = _fallbackResolver.Resolve(rootTranslations, requestedLanguage.Id, defaultLanguage.Id);
                string? initialStatusName = null;

                if (entity.InitialTransactionStatusId.HasValue)
                {
                    List<TransactionStatusTranslation> statusRootTranslations = statusTranslations.Where(x => x.TransactionStatusId == entity.InitialTransactionStatusId.Value).ToList();
                    TransactionStatusTranslation? displayStatusTranslation = _fallbackResolver.Resolve(statusRootTranslations, requestedLanguage.Id, defaultLanguage.Id);
                    initialStatusName = displayStatusTranslation?.Name;
                }

                return new GetListWorkflowTemplateListItemDto
                {
                    Id = entity.Id,
                    Code = entity.Code,
                    InitialTransactionStatusId = entity.InitialTransactionStatusId,
                    InitialTransactionStatusName = initialStatusName,
                    IsDefault = entity.IsDefault,
                    IsActive = entity.IsActive,
                    Name = displayTranslation?.Name ?? string.Empty,
                    Description = displayTranslation?.Description,
                    DisplayLanguageId = displayTranslation?.LanguageId ?? default,
                    IsFallback = requestedTranslation is null && displayTranslation is not null
                };
            }).ToList();

            return new GetListResponse<GetListWorkflowTemplateListItemDto>
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

        private async Task<ApplicationLanguageDto> ResolveRequestedLanguageAsync(Guid? languageId, string? culture, ApplicationLanguageDto defaultLanguage, CancellationToken cancellationToken)
        {
            if (languageId.HasValue)
                return await _languageProvider.GetByIdAsync(languageId.Value, cancellationToken) ?? defaultLanguage;
            if (!string.IsNullOrWhiteSpace(culture))
                return await _languageProvider.GetByCultureAsync(culture, cancellationToken) ?? defaultLanguage;
            return await _currentLanguageProvider.GetCurrentLanguageAsync(cancellationToken);
        }
    }
}

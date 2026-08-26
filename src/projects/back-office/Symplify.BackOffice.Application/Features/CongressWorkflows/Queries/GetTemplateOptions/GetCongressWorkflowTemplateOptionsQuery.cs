using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressWorkflows.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.CongressWorkflows.Queries.GetTemplateOptions;

public sealed class GetCongressWorkflowTemplateOptionsQuery
    : IRequest<GetCongressWorkflowTemplateOptionsResponse>, ISecuredRequest, ICachableRequest
{
    public Guid? LanguageId { get; set; }
    public string? Culture { get; set; }
    public bool OnlyActive { get; set; } = true;

    public string[] Roles => new[] { CongressWorkflowsOperationClaims.Admin, CongressWorkflowsOperationClaims.Read };
    public bool BypassCache { get; }
    public string CacheKey => $"GetCongressWorkflowTemplateOptions({LanguageId},{Culture},{OnlyActive})";
    public string CacheGroupKey => "GetCongressWorkflowTemplateOptions";
    public TimeSpan? SlidingExpiration { get; }

    public sealed class GetCongressWorkflowTemplateOptionsQueryHandler
        : IRequestHandler<GetCongressWorkflowTemplateOptionsQuery, GetCongressWorkflowTemplateOptionsResponse>
    {
        private readonly IWorkflowTemplateRepository _workflowTemplateRepository;
        private readonly IWorkflowTemplateTranslationRepository _workflowTemplateTranslationRepository;
        private readonly ITransactionStatusTranslationRepository _transactionStatusTranslationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public GetCongressWorkflowTemplateOptionsQueryHandler(
            IWorkflowTemplateRepository workflowTemplateRepository,
            IWorkflowTemplateTranslationRepository workflowTemplateTranslationRepository,
            ITransactionStatusTranslationRepository transactionStatusTranslationRepository,
            IApplicationLanguageProvider languageProvider,
            ICurrentLanguageProvider currentLanguageProvider,
            ITranslationFallbackResolver fallbackResolver)
        {
            _workflowTemplateRepository = workflowTemplateRepository;
            _workflowTemplateTranslationRepository = workflowTemplateTranslationRepository;
            _transactionStatusTranslationRepository = transactionStatusTranslationRepository;
            _languageProvider = languageProvider;
            _currentLanguageProvider = currentLanguageProvider;
            _fallbackResolver = fallbackResolver;
        }

        public async Task<GetCongressWorkflowTemplateOptionsResponse> Handle(
            GetCongressWorkflowTemplateOptionsQuery request,
            CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(request.LanguageId, request.Culture, defaultLanguage, cancellationToken);

            IQueryable<WorkflowTemplate> query = _workflowTemplateRepository.Query();

            if (request.OnlyActive)
                query = query.Where(template => template.IsActive);

            List<WorkflowTemplate> templates = query
                .OrderByDescending(template => template.IsDefault)
                .ThenBy(template => template.Code)
                .ThenBy(template => template.Id)
                .ToList();

            HashSet<Guid> templateIds = templates.Select(template => template.Id).ToHashSet();
            List<WorkflowTemplateTranslation> translations = templateIds.Count == 0
                ? new()
                : _workflowTemplateTranslationRepository.Query()
                    .Where(translation => templateIds.Contains(translation.WorkflowTemplateId))
                    .ToList();

            HashSet<int> initialStatusIds = templates
                .Where(template => template.InitialTransactionStatusId.HasValue)
                .Select(template => template.InitialTransactionStatusId!.Value)
                .ToHashSet();

            List<TransactionStatusTranslation> statusTranslations = initialStatusIds.Count == 0
                ? new()
                : _transactionStatusTranslationRepository.Query()
                    .Where(translation => initialStatusIds.Contains(translation.TransactionStatusId))
                    .ToList();

            return new GetCongressWorkflowTemplateOptionsResponse
            {
                Items = templates.Select(template =>
                {
                    List<WorkflowTemplateTranslation> rootTranslations = translations
                        .Where(translation => translation.WorkflowTemplateId == template.Id)
                        .ToList();

                    WorkflowTemplateTranslation? requestedTranslation = rootTranslations
                        .FirstOrDefault(translation => translation.LanguageId == requestedLanguage.Id);

                    WorkflowTemplateTranslation? displayTranslation = _fallbackResolver.Resolve(
                        rootTranslations,
                        requestedLanguage.Id,
                        defaultLanguage.Id);

                    string? initialStatusName = null;

                    if (template.InitialTransactionStatusId.HasValue)
                    {
                        TransactionStatusTranslation? displayStatusTranslation = _fallbackResolver.Resolve(
                            statusTranslations
                                .Where(translation => translation.TransactionStatusId == template.InitialTransactionStatusId.Value)
                                .ToList(),
                            requestedLanguage.Id,
                            defaultLanguage.Id);

                        initialStatusName = displayStatusTranslation?.Name;
                    }

                    return new CongressWorkflowTemplateOptionDto
                    {
                        Id = template.Id,
                        Code = template.Code,
                        Name = displayTranslation?.Name ?? template.Code,
                        Description = displayTranslation?.Description,
                        InitialTransactionStatusId = template.InitialTransactionStatusId,
                        InitialTransactionStatusName = initialStatusName,
                        IsDefault = template.IsDefault,
                        IsActive = template.IsActive,
                        DisplayLanguageId = displayTranslation?.LanguageId ?? default,
                        IsFallback = requestedTranslation is null && displayTranslation is not null
                    };
                }).ToList()
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
    }
}

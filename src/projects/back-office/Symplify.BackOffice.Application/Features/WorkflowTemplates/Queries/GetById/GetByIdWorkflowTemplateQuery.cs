using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.WorkflowTemplates.Constants;
using Symplify.BackOffice.Application.Features.WorkflowTemplates.Rules;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.WorkflowTemplates.Queries.GetById;

public class GetByIdWorkflowTemplateQuery : IRequest<GetByIdWorkflowTemplateResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public Guid? LanguageId { get; set; }
    public string? Culture { get; set; }
    public string[] Roles => new[] { WorkflowTemplatesOperationClaims.Admin, WorkflowTemplatesOperationClaims.Read };

    public class GetByIdWorkflowTemplateQueryHandler : IRequestHandler<GetByIdWorkflowTemplateQuery, GetByIdWorkflowTemplateResponse>
    {
        private readonly IWorkflowTemplateRepository _repository;
        private readonly IWorkflowTemplateTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;
        private readonly WorkflowTemplateBusinessRules _rules;

        public GetByIdWorkflowTemplateQueryHandler(IWorkflowTemplateRepository repository, IWorkflowTemplateTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider, ICurrentLanguageProvider currentLanguageProvider, ITranslationFallbackResolver fallbackResolver, WorkflowTemplateBusinessRules rules)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _languageProvider = languageProvider;
            _currentLanguageProvider = currentLanguageProvider;
            _fallbackResolver = fallbackResolver;
            _rules = rules;
        }

        public async Task<GetByIdWorkflowTemplateResponse> Handle(GetByIdWorkflowTemplateQuery request, CancellationToken cancellationToken)
        {
            WorkflowTemplate? entity = await _repository.GetAsync(predicate: x => x.Id == request.Id, cancellationToken: cancellationToken);
            await _rules.WorkflowTemplateShouldExistWhenSelected(entity);
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = request.LanguageId.HasValue ? await _languageProvider.GetByIdAsync(request.LanguageId.Value, cancellationToken) ?? defaultLanguage : !string.IsNullOrWhiteSpace(request.Culture) ? await _languageProvider.GetByCultureAsync(request.Culture, cancellationToken) ?? defaultLanguage : await _currentLanguageProvider.GetCurrentLanguageAsync(cancellationToken);
            List<WorkflowTemplateTranslation> translations = _translationRepository.Query().Where(translation => translation.WorkflowTemplateId == request.Id).ToList();
            WorkflowTemplateTranslation? requestedTranslation = translations.FirstOrDefault(translation => translation.LanguageId == requestedLanguage.Id);
            WorkflowTemplateTranslation? displayTranslation = _fallbackResolver.Resolve(translations, requestedLanguage.Id, defaultLanguage.Id);

            return new GetByIdWorkflowTemplateResponse
            {
                Id = entity!.Id,
                Code = entity.Code,
                InitialTransactionStatusId = entity.InitialTransactionStatusId,
                IsDefault = entity.IsDefault,
                IsActive = entity.IsActive,
                Name = displayTranslation?.Name ?? string.Empty,
                Description = displayTranslation?.Description,
                DisplayLanguageId = displayTranslation?.LanguageId ?? default,
                IsFallback = requestedTranslation is null && displayTranslation is not null
            };
        }
    }
}

using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.WorkflowTemplates.Constants;
using Symplify.BackOffice.Application.Features.WorkflowTemplates.Rules;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.WorkflowTemplates.Queries.GetForUpdate;

public class GetWorkflowTemplateForUpdateQuery : IRequest<GetWorkflowTemplateForUpdateResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { WorkflowTemplatesOperationClaims.Admin, WorkflowTemplatesOperationClaims.Read };

    public class GetWorkflowTemplateForUpdateQueryHandler : IRequestHandler<GetWorkflowTemplateForUpdateQuery, GetWorkflowTemplateForUpdateResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name", "Description" };
        private readonly IWorkflowTemplateRepository _repository;
        private readonly IWorkflowTemplateTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly WorkflowTemplateBusinessRules _rules;

        public GetWorkflowTemplateForUpdateQueryHandler(IWorkflowTemplateRepository repository, IWorkflowTemplateTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider, WorkflowTemplateBusinessRules rules)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _languageProvider = languageProvider;
            _rules = rules;
        }

        public async Task<GetWorkflowTemplateForUpdateResponse> Handle(GetWorkflowTemplateForUpdateQuery request, CancellationToken cancellationToken)
        {
            WorkflowTemplate? entity = await _repository.GetAsync(predicate: x => x.Id == request.Id, cancellationToken: cancellationToken);
            await _rules.WorkflowTemplateShouldExistWhenSelected(entity);

            IReadOnlyList<ApplicationLanguageDto> languages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            List<WorkflowTemplateTranslation> translations = _translationRepository.Query().Where(translation => translation.WorkflowTemplateId == request.Id).ToList();

            return new GetWorkflowTemplateForUpdateResponse
            {
                Id = entity!.Id,
                Code = entity.Code,
                InitialTransactionStatusId = entity.InitialTransactionStatusId,
                IsDefault = entity.IsDefault,
                IsActive = entity.IsActive,
                Translations = languages.Select(language =>
                {
                    WorkflowTemplateTranslation? translation = translations.FirstOrDefault(item => item.LanguageId == language.Id);
                    return new LocalizedTranslationDto
                    {
                        LanguageId = language.Id,
                        Culture = language.Culture,
                        LanguageName = language.Name,
                        IsDefault = language.IsDefault,
                        Exists = translation is not null,
                        Fields = TranslationFieldNames.ToDictionary(field => field, field => translation is null ? null : LocalizedEntityRuntimeHelper.GetPropertyValue(translation, field)?.ToString(), StringComparer.OrdinalIgnoreCase)
                    };
                }).ToList()
            };
        }
    }
}

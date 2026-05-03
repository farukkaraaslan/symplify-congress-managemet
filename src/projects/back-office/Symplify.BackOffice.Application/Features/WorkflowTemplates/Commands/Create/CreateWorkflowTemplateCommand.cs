using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.WorkflowTemplates.Constants;
using Symplify.BackOffice.Application.Features.WorkflowTemplates.Rules;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.WorkflowTemplates.Commands.Create;

public class CreateWorkflowTemplateCommand : IRequest<CreatedWorkflowTemplateResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public string Code { get; set; } = string.Empty;
    public int? InitialTransactionStatusId { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetWorkflowTemplates";
    public string[] Roles => new[] { WorkflowTemplatesOperationClaims.Admin, WorkflowTemplatesOperationClaims.Write, WorkflowTemplatesOperationClaims.Add };

    public class CreateWorkflowTemplateCommandHandler : IRequestHandler<CreateWorkflowTemplateCommand, CreatedWorkflowTemplateResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name", "Description" };
        private readonly IWorkflowTemplateRepository _repository;
        private readonly IWorkflowTemplateTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly WorkflowTemplateBusinessRules _rules;

        public CreateWorkflowTemplateCommandHandler(IWorkflowTemplateRepository repository, IWorkflowTemplateTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider, WorkflowTemplateBusinessRules rules)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _languageProvider = languageProvider;
            _rules = rules;
        }

        public async Task<CreatedWorkflowTemplateResponse> Handle(CreateWorkflowTemplateCommand request, CancellationToken cancellationToken)
        {
            await _rules.DefaultTranslationShouldExist(request.Translations, cancellationToken);
            await _rules.TranslationNamesShouldBeUniqueInRequest(request.Translations);
            await _rules.TranslationNamesShouldBeUniqueInDatabase(request.Translations, null);
            await _rules.CodeShouldBeUniqueWhenCreating(request.Code);
            await _rules.InitialStatusShouldBeValid(request.InitialTransactionStatusId);

            if (request.IsDefault)
                await ClearDefaultTemplatesAsync(null);

            WorkflowTemplate entity = new()
            {
                Id = Guid.NewGuid(),
                Code = NormalizeCode(request.Code),
                InitialTransactionStatusId = request.InitialTransactionStatusId,
                IsDefault = request.IsDefault,
                IsActive = request.IsActive
            };

            WorkflowTemplate created = await _repository.AddAsync(entity);
            await CreateTranslationsAsync(created.Id, request.Translations, cancellationToken);

            return new CreatedWorkflowTemplateResponse { Id = created.Id, Code = created.Code, InitialTransactionStatusId = created.InitialTransactionStatusId, IsDefault = created.IsDefault, IsActive = created.IsActive };
        }

        private async Task CreateTranslationsAsync(Guid rootId, IEnumerable<TranslationInputDto> translations, CancellationToken cancellationToken)
        {
            IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            HashSet<Guid> activeLanguageIds = activeLanguages.Select(language => language.Id).ToHashSet();
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);

            foreach (TranslationInputDto input in translations)
            {
                if (!activeLanguageIds.Contains(input.LanguageId)) continue;
                bool isDefaultLanguage = input.LanguageId == defaultLanguage.Id;
                bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(input.Fields, TranslationFieldNames);
                if (!isDefaultLanguage && !hasAnyValue) continue;

                WorkflowTemplateTranslation translation = new();
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "Id", Guid.NewGuid());
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "WorkflowTemplateId", rootId);
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "LanguageId", input.LanguageId);
                LocalizedEntityRuntimeHelper.ApplyFieldDictionary(translation, TranslationFieldNames, input.Fields);
                await _translationRepository.AddAsync(translation);
            }
        }

        private async Task ClearDefaultTemplatesAsync(Guid? excludedId)
        {
            List<WorkflowTemplate> defaults = _repository.Query().ToList().Where(entity => entity.IsDefault && (!excludedId.HasValue || entity.Id != excludedId.Value)).ToList();
            foreach (WorkflowTemplate item in defaults)
            {
                item.IsDefault = false;
                await _repository.UpdateAsync(item);
            }
        }

        private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
    }
}

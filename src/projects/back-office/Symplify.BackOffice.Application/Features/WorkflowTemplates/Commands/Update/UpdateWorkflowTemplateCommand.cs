using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.WorkflowTemplates.Constants;
using Symplify.BackOffice.Application.Features.WorkflowTemplates.Rules;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.WorkflowTemplates.Commands.Update;

public class UpdateWorkflowTemplateCommand : IRequest<UpdatedWorkflowTemplateResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int? InitialTransactionStatusId { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetWorkflowTemplates";
    public string[] Roles => new[] { WorkflowTemplatesOperationClaims.Admin, WorkflowTemplatesOperationClaims.Write, WorkflowTemplatesOperationClaims.Update };

    public class UpdateWorkflowTemplateCommandHandler : IRequestHandler<UpdateWorkflowTemplateCommand, UpdatedWorkflowTemplateResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name", "Description" };
        private readonly IWorkflowTemplateRepository _repository;
        private readonly IWorkflowTemplateTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly WorkflowTemplateBusinessRules _rules;

        public UpdateWorkflowTemplateCommandHandler(IWorkflowTemplateRepository repository, IWorkflowTemplateTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider, WorkflowTemplateBusinessRules rules)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _languageProvider = languageProvider;
            _rules = rules;
        }

        public async Task<UpdatedWorkflowTemplateResponse> Handle(UpdateWorkflowTemplateCommand request, CancellationToken cancellationToken)
        {
            WorkflowTemplate? entity = await _repository.GetAsync(predicate: x => x.Id == request.Id, cancellationToken: cancellationToken);
            await _rules.WorkflowTemplateShouldExistWhenSelected(entity);
            await _rules.DefaultTranslationShouldExist(request.Translations, cancellationToken);
            await _rules.TranslationNamesShouldBeUniqueInRequest(request.Translations);
            await _rules.TranslationNamesShouldBeUniqueInDatabase(request.Translations, request.Id);
            await _rules.CodeShouldBeUniqueWhenUpdating(request.Id, request.Code);
            await _rules.InitialStatusShouldBeValid(request.InitialTransactionStatusId);

            if (request.IsDefault)
                await ClearDefaultTemplatesAsync(request.Id);

            entity!.Code = NormalizeCode(request.Code);
            entity.InitialTransactionStatusId = request.InitialTransactionStatusId;
            entity.IsDefault = request.IsDefault;
            entity.IsActive = request.IsActive;

            WorkflowTemplate updated = await _repository.UpdateAsync(entity);
            await UpsertTranslationsAsync(updated.Id, request.Translations, cancellationToken);

            return new UpdatedWorkflowTemplateResponse { Id = updated.Id, Code = updated.Code, InitialTransactionStatusId = updated.InitialTransactionStatusId, IsDefault = updated.IsDefault, IsActive = updated.IsActive };
        }

        private async Task UpsertTranslationsAsync(Guid rootId, IEnumerable<TranslationInputDto> translations, CancellationToken cancellationToken)
        {
            IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            HashSet<Guid> activeLanguageIds = activeLanguages.Select(language => language.Id).ToHashSet();
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            List<WorkflowTemplateTranslation> existingTranslations = _translationRepository.Query().Where(translation => translation.WorkflowTemplateId == rootId).ToList();

            foreach (TranslationInputDto input in translations)
            {
                if (!activeLanguageIds.Contains(input.LanguageId)) continue;
                bool isDefaultLanguage = input.LanguageId == defaultLanguage.Id;
                bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(input.Fields, TranslationFieldNames);
                if (!isDefaultLanguage && !hasAnyValue) continue;

                WorkflowTemplateTranslation? existing = existingTranslations.FirstOrDefault(translation => translation.LanguageId == input.LanguageId);
                if (existing is null)
                {
                    WorkflowTemplateTranslation translation = new();
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "Id", Guid.NewGuid());
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "WorkflowTemplateId", rootId);
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "LanguageId", input.LanguageId);
                    LocalizedEntityRuntimeHelper.ApplyFieldDictionary(translation, TranslationFieldNames, input.Fields);
                    await _translationRepository.AddAsync(translation);
                    continue;
                }

                LocalizedEntityRuntimeHelper.ApplyFieldDictionary(existing, TranslationFieldNames, input.Fields);
                await _translationRepository.UpdateAsync(existing);
            }
        }

        private async Task ClearDefaultTemplatesAsync(Guid excludedId)
        {
            List<WorkflowTemplate> defaults = _repository.Query().ToList().Where(entity => entity.IsDefault && entity.Id != excludedId).ToList();
            foreach (WorkflowTemplate item in defaults)
            {
                item.IsDefault = false;
                await _repository.UpdateAsync(item);
            }
        }

        private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
    }
}

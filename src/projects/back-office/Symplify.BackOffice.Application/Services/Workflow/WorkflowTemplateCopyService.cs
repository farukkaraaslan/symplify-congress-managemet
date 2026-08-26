using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Services.Workflow;

public sealed class WorkflowTemplateCopyService : IWorkflowTemplateCopyService
{
    private readonly IWorkflowTemplateRepository _templateRepository;
    private readonly IWorkflowTemplateTransitionRepository _templateTransitionRepository;
    private readonly ICongressWorkflowSettingRepository _congressWorkflowSettingRepository;
    private readonly ICongressTransactionStatusTransitionRepository _congressTransitionRepository;

    public WorkflowTemplateCopyService(
        IWorkflowTemplateRepository templateRepository,
        IWorkflowTemplateTransitionRepository templateTransitionRepository,
        ICongressWorkflowSettingRepository congressWorkflowSettingRepository,
        ICongressTransactionStatusTransitionRepository congressTransitionRepository)
    {
        _templateRepository = templateRepository;
        _templateTransitionRepository = templateTransitionRepository;
        _congressWorkflowSettingRepository = congressWorkflowSettingRepository;
        _congressTransitionRepository = congressTransitionRepository;
    }

    public async Task ApplyTemplateToCongressAsync(
        Guid congressId,
        Guid workflowTemplateId,
        bool replaceExistingTransitions,
        CancellationToken cancellationToken = default)
    {
        WorkflowTemplate? template = await _templateRepository.GetAsync(
            predicate: x => x.Id == workflowTemplateId,
            cancellationToken: cancellationToken);

        if (template is null)
            return;

        CongressWorkflowSetting? setting = await _congressWorkflowSettingRepository.GetAsync(
            predicate: x => x.CongressId == congressId,
            cancellationToken: cancellationToken);

        if (setting is null)
        {
            setting = new CongressWorkflowSetting
            {
                Id = Guid.NewGuid(),
                CongressId = congressId,
                SourceWorkflowTemplateId = workflowTemplateId,
                InitialTransactionStatusId = template.InitialTransactionStatusId,
                IsActive = true
            };

            await _congressWorkflowSettingRepository.AddAsync(setting);
        }
        else
        {
            setting.SourceWorkflowTemplateId = workflowTemplateId;
            setting.InitialTransactionStatusId = template.InitialTransactionStatusId;
            setting.IsActive = true;

            await _congressWorkflowSettingRepository.UpdateAsync(setting);
        }

        if (replaceExistingTransitions)
        {
            List<CongressTransactionStatusTransition> existingTransitions = _congressTransitionRepository.Query()
                .ToList()
                .Where(entity => entity.CongressId == congressId && !IsDeleted(entity))
                .ToList();

            foreach (CongressTransactionStatusTransition existing in existingTransitions)
                await _congressTransitionRepository.DeleteAsync(existing);
        }

        List<WorkflowTemplateTransition> templateTransitions = _templateTransitionRepository.Query()
            .ToList()
            .Where(entity => entity.WorkflowTemplateId == workflowTemplateId && entity.IsActive && !IsDeleted(entity))
            .OrderBy(entity => entity.Order)
            .ThenBy(entity => entity.Id)
            .ToList();

        List<CongressTransactionStatusTransition> congressTransitions = _congressTransitionRepository.Query()
            .ToList()
            .Where(entity => entity.CongressId == congressId && !IsDeleted(entity))
            .ToList();

        foreach (WorkflowTemplateTransition templateTransition in templateTransitions)
        {
            bool exists = congressTransitions.Any(entity => entity.TransactionStatusTransitionId == templateTransition.TransactionStatusTransitionId);

            if (exists)
                continue;

            CongressTransactionStatusTransition congressTransition = new()
            {
                Id = Guid.NewGuid(),
                CongressId = congressId,
                TransactionStatusTransitionId = templateTransition.TransactionStatusTransitionId,
                SourceWorkflowTemplateTransitionId = templateTransition.Id,
                Order = templateTransition.Order,
                IsActive = templateTransition.IsActive
            };

            await _congressTransitionRepository.AddAsync(congressTransition);
        }
    }

    private static bool IsDeleted(object entity)
    {
        object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate");
        return deletedDate is not null;
    }
}

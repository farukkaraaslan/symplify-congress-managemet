using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressWorkflows.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.CongressWorkflows.Rules;

public class CongressWorkflowBusinessRules : BaseBusinessRules
{
    private readonly ICongressRepository _congressRepository;
    private readonly IWorkflowTemplateRepository _workflowTemplateRepository;
    private readonly IWorkflowTemplateTransitionRepository _workflowTemplateTransitionRepository;
    private readonly ITransactionStatusTransitionRepository _transactionStatusTransitionRepository;
    private readonly ICongressTransactionStatusTransitionRepository _congressTransitionRepository;

    public CongressWorkflowBusinessRules(
        ICongressRepository congressRepository,
        IWorkflowTemplateRepository workflowTemplateRepository,
        IWorkflowTemplateTransitionRepository workflowTemplateTransitionRepository,
        ITransactionStatusTransitionRepository transactionStatusTransitionRepository,
        ICongressTransactionStatusTransitionRepository congressTransitionRepository)
    {
        _congressRepository = congressRepository;
        _workflowTemplateRepository = workflowTemplateRepository;
        _workflowTemplateTransitionRepository = workflowTemplateTransitionRepository;
        _transactionStatusTransitionRepository = transactionStatusTransitionRepository;
        _congressTransitionRepository = congressTransitionRepository;
    }

    public Task CongressShouldExist(Guid congressId)
    {
        bool exists = _congressRepository.Query().ToList().Where(entity => !IsDeleted(entity)).Any(entity => entity.Id == congressId);
        if (!exists)
            throw new BusinessException(CongressWorkflowsMessages.CongressNotFound);
        return Task.CompletedTask;
    }

    public Task WorkflowTemplateShouldExistAndBeActive(Guid workflowTemplateId)
    {
        WorkflowTemplate? template = _workflowTemplateRepository.Query().ToList().Where(entity => !IsDeleted(entity)).FirstOrDefault(entity => entity.Id == workflowTemplateId);
        if (template is null || !template.IsActive)
            throw new BusinessException(CongressWorkflowsMessages.TemplateNotFound);

        bool hasTransitions = _workflowTemplateTransitionRepository.Query()
            .ToList()
            .Where(entity => !IsDeleted(entity))
            .Any(entity => entity.WorkflowTemplateId == workflowTemplateId && entity.IsActive);

        if (!hasTransitions)
            throw new BusinessException(CongressWorkflowsMessages.TemplateHasNoTransitions);

        if (!template.InitialTransactionStatusId.HasValue)
            throw new BusinessException(CongressWorkflowsMessages.InitialStatusRequired);

        return Task.CompletedTask;
    }

    public Task TransactionStatusTransitionShouldExistAndBeActive(int transitionId)
    {
        TransactionStatusTransition? transition = _transactionStatusTransitionRepository.Query()
            .ToList()
            .Where(entity => !IsDeleted(entity))
            .FirstOrDefault(entity => entity.Id == transitionId);

        if (transition is null || !transition.IsActive)
            throw new BusinessException(CongressWorkflowsMessages.TransitionNotFound);

        return Task.CompletedTask;
    }

    public Task TransitionShouldBeUniqueInCongress(Guid congressId, int transitionId, Guid? excludedId)
    {
        bool exists = _congressTransitionRepository.Query()
            .ToList()
            .Where(entity => !IsDeleted(entity))
            .Any(entity => entity.CongressId == congressId && entity.TransactionStatusTransitionId == transitionId && (!excludedId.HasValue || entity.Id != excludedId.Value));

        if (exists)
            throw new BusinessException(CongressWorkflowsMessages.TransitionAlreadyExists);

        return Task.CompletedTask;
    }

    public Task CongressTransitionShouldExistWhenSelected(CongressTransactionStatusTransition? entity)
    {
        if (entity is null)
            throw new BusinessException(CongressWorkflowsMessages.EntityNotFound);
        return Task.CompletedTask;
    }

    private static bool IsDeleted(object entity)
    {
        object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate");
        return deletedDate is not null;
    }
}

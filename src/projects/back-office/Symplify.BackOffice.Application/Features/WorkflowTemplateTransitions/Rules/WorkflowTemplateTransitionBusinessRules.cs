using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.WorkflowTemplateTransitions.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.WorkflowTemplateTransitions.Rules;

public class WorkflowTemplateTransitionBusinessRules : BaseBusinessRules
{
    private readonly IWorkflowTemplateRepository _templateRepository;
    private readonly IWorkflowTemplateTransitionRepository _templateTransitionRepository;
    private readonly ITransactionStatusTransitionRepository _transactionStatusTransitionRepository;
    private readonly ICongressTransactionStatusTransitionRepository _congressTransactionStatusTransitionRepository;

    public WorkflowTemplateTransitionBusinessRules(
        IWorkflowTemplateRepository templateRepository,
        IWorkflowTemplateTransitionRepository templateTransitionRepository,
        ITransactionStatusTransitionRepository transactionStatusTransitionRepository,
        ICongressTransactionStatusTransitionRepository congressTransactionStatusTransitionRepository)
    {
        _templateRepository = templateRepository;
        _templateTransitionRepository = templateTransitionRepository;
        _transactionStatusTransitionRepository = transactionStatusTransitionRepository;
        _congressTransactionStatusTransitionRepository = congressTransactionStatusTransitionRepository;
    }

    public Task WorkflowTemplateTransitionShouldExistWhenSelected(WorkflowTemplateTransition? entity)
    {
        if (entity is null)
            throw new BusinessException(WorkflowTemplateTransitionsMessages.EntityNotFound);
        return Task.CompletedTask;
    }

    public Task WorkflowTemplateShouldExist(Guid workflowTemplateId)
    {
        bool exists = _templateRepository.Query().ToList().Where(entity => !IsDeleted(entity)).Any(entity => entity.Id == workflowTemplateId);
        if (!exists)
            throw new BusinessException(WorkflowTemplateTransitionsMessages.TemplateNotFound);
        return Task.CompletedTask;
    }

    public Task TransactionStatusTransitionShouldExistAndBeActive(int transitionId)
    {
        TransactionStatusTransition? transition = _transactionStatusTransitionRepository.Query()
            .ToList()
            .Where(entity => !IsDeleted(entity))
            .FirstOrDefault(entity => entity.Id == transitionId);

        if (transition is null)
            throw new BusinessException(WorkflowTemplateTransitionsMessages.TransitionNotFound);
        if (!transition.IsActive)
            throw new BusinessException(WorkflowTemplateTransitionsMessages.TransitionInactive);
        return Task.CompletedTask;
    }

    public Task TransitionShouldBeUniqueInTemplate(Guid workflowTemplateId, int transitionId, Guid? excludedId)
    {
        bool exists = _templateTransitionRepository.Query()
            .ToList()
            .Where(entity => !IsDeleted(entity))
            .Any(entity => entity.WorkflowTemplateId == workflowTemplateId && entity.TransactionStatusTransitionId == transitionId && (!excludedId.HasValue || entity.Id != excludedId.Value));

        if (exists)
            throw new BusinessException(WorkflowTemplateTransitionsMessages.TransitionAlreadyExists);
        return Task.CompletedTask;
    }

    public Task WorkflowTemplateTransitionShouldNotBeUsedByCongress(Guid workflowTemplateTransitionId)
    {
        bool isUsed = _congressTransactionStatusTransitionRepository.Query()
            .ToList()
            .Where(entity => !IsDeleted(entity))
            .Any(entity => entity.SourceWorkflowTemplateTransitionId == workflowTemplateTransitionId);

        if (isUsed)
            throw new BusinessException(WorkflowTemplateTransitionsMessages.EntityUsedByCongressWorkflow);
        return Task.CompletedTask;
    }

    private static bool IsDeleted(object entity)
    {
        object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate");
        return deletedDate is not null;
    }
}

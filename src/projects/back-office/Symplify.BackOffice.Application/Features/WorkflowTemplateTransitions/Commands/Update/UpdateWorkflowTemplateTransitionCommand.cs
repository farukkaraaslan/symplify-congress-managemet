using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.WorkflowTemplateTransitions.Constants;
using Symplify.BackOffice.Application.Features.WorkflowTemplateTransitions.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.WorkflowTemplateTransitions.Commands.Update;

public class UpdateWorkflowTemplateTransitionCommand : IRequest<UpdatedWorkflowTemplateTransitionResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public int TransactionStatusTransitionId { get; set; }
    public bool IsActive { get; set; } = true;

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetWorkflowTemplates";
    public string[] Roles => new[] { WorkflowTemplateTransitionsOperationClaims.Admin, WorkflowTemplateTransitionsOperationClaims.Write, WorkflowTemplateTransitionsOperationClaims.Update };

    public class UpdateWorkflowTemplateTransitionCommandHandler : IRequestHandler<UpdateWorkflowTemplateTransitionCommand, UpdatedWorkflowTemplateTransitionResponse>
    {
        private readonly IWorkflowTemplateTransitionRepository _repository;
        private readonly WorkflowTemplateTransitionBusinessRules _rules;

        public UpdateWorkflowTemplateTransitionCommandHandler(IWorkflowTemplateTransitionRepository repository, WorkflowTemplateTransitionBusinessRules rules)
        {
            _repository = repository;
            _rules = rules;
        }

        public async Task<UpdatedWorkflowTemplateTransitionResponse> Handle(UpdateWorkflowTemplateTransitionCommand request, CancellationToken cancellationToken)
        {
            WorkflowTemplateTransition? entity = await _repository.GetAsync(predicate: x => x.Id == request.Id, cancellationToken: cancellationToken);
            await _rules.WorkflowTemplateTransitionShouldExistWhenSelected(entity);
            await _rules.TransactionStatusTransitionShouldExistAndBeActive(request.TransactionStatusTransitionId);
            await _rules.TransitionShouldBeUniqueInTemplate(entity!.WorkflowTemplateId, request.TransactionStatusTransitionId, request.Id);

            entity.TransactionStatusTransitionId = request.TransactionStatusTransitionId;
            entity.IsActive = request.IsActive;

            WorkflowTemplateTransition updated = await _repository.UpdateAsync(entity);
            return new UpdatedWorkflowTemplateTransitionResponse { Id = updated.Id, WorkflowTemplateId = updated.WorkflowTemplateId, TransactionStatusTransitionId = updated.TransactionStatusTransitionId, Order = updated.Order, IsActive = updated.IsActive };
        }
    }
}

using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.WorkflowTemplateTransitions.Constants;
using Symplify.BackOffice.Application.Features.WorkflowTemplateTransitions.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.WorkflowTemplateTransitions.Commands.Create;

public class CreateWorkflowTemplateTransitionCommand : IRequest<CreatedWorkflowTemplateTransitionResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid WorkflowTemplateId { get; set; }
    public int TransactionStatusTransitionId { get; set; }
    public bool IsActive { get; set; } = true;

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetWorkflowTemplates";
    public string[] Roles => new[] { WorkflowTemplateTransitionsOperationClaims.Admin, WorkflowTemplateTransitionsOperationClaims.Write, WorkflowTemplateTransitionsOperationClaims.Add };

    public class CreateWorkflowTemplateTransitionCommandHandler : IRequestHandler<CreateWorkflowTemplateTransitionCommand, CreatedWorkflowTemplateTransitionResponse>
    {
        private readonly IWorkflowTemplateTransitionRepository _repository;
        private readonly WorkflowTemplateTransitionBusinessRules _rules;

        public CreateWorkflowTemplateTransitionCommandHandler(IWorkflowTemplateTransitionRepository repository, WorkflowTemplateTransitionBusinessRules rules)
        {
            _repository = repository;
            _rules = rules;
        }

        public async Task<CreatedWorkflowTemplateTransitionResponse> Handle(CreateWorkflowTemplateTransitionCommand request, CancellationToken cancellationToken)
        {
            await _rules.WorkflowTemplateShouldExist(request.WorkflowTemplateId);
            await _rules.TransactionStatusTransitionShouldExistAndBeActive(request.TransactionStatusTransitionId);
            await _rules.TransitionShouldBeUniqueInTemplate(request.WorkflowTemplateId, request.TransactionStatusTransitionId, null);

            int nextOrder = _repository.Query().ToList().Where(entity => entity.WorkflowTemplateId == request.WorkflowTemplateId).Select(entity => entity.Order).DefaultIfEmpty(0).Max() + 1;

            WorkflowTemplateTransition entity = new()
            {
                Id = Guid.NewGuid(),
                WorkflowTemplateId = request.WorkflowTemplateId,
                TransactionStatusTransitionId = request.TransactionStatusTransitionId,
                Order = nextOrder,
                IsActive = request.IsActive
            };

            WorkflowTemplateTransition created = await _repository.AddAsync(entity);

            return new CreatedWorkflowTemplateTransitionResponse { Id = created.Id, WorkflowTemplateId = created.WorkflowTemplateId, TransactionStatusTransitionId = created.TransactionStatusTransitionId, Order = created.Order, IsActive = created.IsActive };
        }
    }
}

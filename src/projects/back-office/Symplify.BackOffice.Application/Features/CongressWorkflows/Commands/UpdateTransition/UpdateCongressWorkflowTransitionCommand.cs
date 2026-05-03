using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressWorkflows.Constants;
using Symplify.BackOffice.Application.Features.CongressWorkflows.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressWorkflows.Commands.UpdateTransition;

public class UpdateCongressWorkflowTransitionCommand : IRequest<UpdatedCongressWorkflowTransitionResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public int TransactionStatusTransitionId { get; set; }
    public bool IsActive { get; set; } = true;

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressWorkflow";
    public string[] Roles => new[] { CongressWorkflowsOperationClaims.Admin, CongressWorkflowsOperationClaims.Write, CongressWorkflowsOperationClaims.Update };

    public class UpdateCongressWorkflowTransitionCommandHandler : IRequestHandler<UpdateCongressWorkflowTransitionCommand, UpdatedCongressWorkflowTransitionResponse>
    {
        private readonly ICongressTransactionStatusTransitionRepository _repository;
        private readonly CongressWorkflowBusinessRules _rules;

        public UpdateCongressWorkflowTransitionCommandHandler(ICongressTransactionStatusTransitionRepository repository, CongressWorkflowBusinessRules rules)
        {
            _repository = repository;
            _rules = rules;
        }

        public async Task<UpdatedCongressWorkflowTransitionResponse> Handle(UpdateCongressWorkflowTransitionCommand request, CancellationToken cancellationToken)
        {
            CongressTransactionStatusTransition? entity = await _repository.GetAsync(predicate: x => x.Id == request.Id, cancellationToken: cancellationToken);
            await _rules.CongressTransitionShouldExistWhenSelected(entity);
            await _rules.TransactionStatusTransitionShouldExistAndBeActive(request.TransactionStatusTransitionId);
            await _rules.TransitionShouldBeUniqueInCongress(entity!.CongressId, request.TransactionStatusTransitionId, request.Id);

            entity.TransactionStatusTransitionId = request.TransactionStatusTransitionId;
            entity.IsActive = request.IsActive;
            CongressTransactionStatusTransition updated = await _repository.UpdateAsync(entity);
            return new UpdatedCongressWorkflowTransitionResponse { Id = updated.Id };
        }
    }
}

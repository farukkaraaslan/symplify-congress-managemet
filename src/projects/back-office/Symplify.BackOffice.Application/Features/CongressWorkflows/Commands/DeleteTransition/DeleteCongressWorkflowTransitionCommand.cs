using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressWorkflows.Constants;
using Symplify.BackOffice.Application.Features.CongressWorkflows.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressWorkflows.Commands.DeleteTransition;

public class DeleteCongressWorkflowTransitionCommand : IRequest<DeletedCongressWorkflowTransitionResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressWorkflow";
    public string[] Roles => new[] { CongressWorkflowsOperationClaims.Admin, CongressWorkflowsOperationClaims.Write, CongressWorkflowsOperationClaims.Delete };

    public class DeleteCongressWorkflowTransitionCommandHandler : IRequestHandler<DeleteCongressWorkflowTransitionCommand, DeletedCongressWorkflowTransitionResponse>
    {
        private readonly ICongressTransactionStatusTransitionRepository _repository;
        private readonly CongressWorkflowBusinessRules _rules;

        public DeleteCongressWorkflowTransitionCommandHandler(ICongressTransactionStatusTransitionRepository repository, CongressWorkflowBusinessRules rules)
        {
            _repository = repository;
            _rules = rules;
        }

        public async Task<DeletedCongressWorkflowTransitionResponse> Handle(DeleteCongressWorkflowTransitionCommand request, CancellationToken cancellationToken)
        {
            CongressTransactionStatusTransition? entity = await _repository.GetAsync(predicate: x => x.Id == request.Id, cancellationToken: cancellationToken);
            await _rules.CongressTransitionShouldExistWhenSelected(entity);
            Guid congressId = entity!.CongressId;
            await _repository.DeleteAsync(entity);
            await NormalizeOrdersAsync(congressId);
            return new DeletedCongressWorkflowTransitionResponse { Id = request.Id };
        }

        private async Task NormalizeOrdersAsync(Guid congressId)
        {
            List<CongressTransactionStatusTransition> items = _repository.Query().ToList().Where(entity => entity.CongressId == congressId).OrderBy(entity => entity.Order).ThenBy(entity => entity.Id).ToList();
            for (int i = 0; i < items.Count; i++)
            {
                int order = i + 1;
                if (items[i].Order == order) continue;
                items[i].Order = order;
                await _repository.UpdateAsync(items[i]);
            }
        }
    }
}

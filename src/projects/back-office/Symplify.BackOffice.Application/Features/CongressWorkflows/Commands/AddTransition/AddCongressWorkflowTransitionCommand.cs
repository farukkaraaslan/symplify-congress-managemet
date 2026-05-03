using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressWorkflows.Constants;
using Symplify.BackOffice.Application.Features.CongressWorkflows.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressWorkflows.Commands.AddTransition;

public class AddCongressWorkflowTransitionCommand : IRequest<AddedCongressWorkflowTransitionResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressId { get; set; }
    public int TransactionStatusTransitionId { get; set; }
    public bool IsActive { get; set; } = true;

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => $"GetCongressWorkflow({CongressId})";
    public string[] Roles => new[] { CongressWorkflowsOperationClaims.Admin, CongressWorkflowsOperationClaims.Write, CongressWorkflowsOperationClaims.Add };

    public class AddCongressWorkflowTransitionCommandHandler : IRequestHandler<AddCongressWorkflowTransitionCommand, AddedCongressWorkflowTransitionResponse>
    {
        private readonly ICongressTransactionStatusTransitionRepository _repository;
        private readonly CongressWorkflowBusinessRules _rules;

        public AddCongressWorkflowTransitionCommandHandler(ICongressTransactionStatusTransitionRepository repository, CongressWorkflowBusinessRules rules)
        {
            _repository = repository;
            _rules = rules;
        }

        public async Task<AddedCongressWorkflowTransitionResponse> Handle(AddCongressWorkflowTransitionCommand request, CancellationToken cancellationToken)
        {
            await _rules.CongressShouldExist(request.CongressId);
            await _rules.TransactionStatusTransitionShouldExistAndBeActive(request.TransactionStatusTransitionId);
            await _rules.TransitionShouldBeUniqueInCongress(request.CongressId, request.TransactionStatusTransitionId, null);

            int nextOrder = _repository.Query().ToList().Where(entity => entity.CongressId == request.CongressId).Select(entity => entity.Order).DefaultIfEmpty(0).Max() + 1;
            CongressTransactionStatusTransition entity = new()
            {
                Id = Guid.NewGuid(),
                CongressId = request.CongressId,
                TransactionStatusTransitionId = request.TransactionStatusTransitionId,
                Order = nextOrder,
                IsActive = request.IsActive
            };

            CongressTransactionStatusTransition created = await _repository.AddAsync(entity);
            return new AddedCongressWorkflowTransitionResponse { Id = created.Id };
        }
    }
}

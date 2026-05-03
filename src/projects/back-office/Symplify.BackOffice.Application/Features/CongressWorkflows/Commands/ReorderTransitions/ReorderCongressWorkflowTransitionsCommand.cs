using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressWorkflows.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressWorkflows.Commands.ReorderTransitions;

public class ReorderCongressWorkflowTransitionsCommand : IRequest<ReorderedCongressWorkflowTransitionsResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressId { get; set; }
    public IList<ReorderCongressWorkflowTransitionItemDto> Items { get; set; } = new List<ReorderCongressWorkflowTransitionItemDto>();

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => $"GetCongressWorkflow({CongressId})";
    public string[] Roles => new[] { CongressWorkflowsOperationClaims.Admin, CongressWorkflowsOperationClaims.Write, CongressWorkflowsOperationClaims.Update };

    public class ReorderCongressWorkflowTransitionsCommandHandler : IRequestHandler<ReorderCongressWorkflowTransitionsCommand, ReorderedCongressWorkflowTransitionsResponse>
    {
        private readonly ICongressTransactionStatusTransitionRepository _repository;

        public ReorderCongressWorkflowTransitionsCommandHandler(ICongressTransactionStatusTransitionRepository repository)
        {
            _repository = repository;
        }

        public async Task<ReorderedCongressWorkflowTransitionsResponse> Handle(ReorderCongressWorkflowTransitionsCommand request, CancellationToken cancellationToken)
        {
            if (request.Items.Count == 0)
                throw new BusinessException(CongressWorkflowsMessages.ReorderInvalid);

            List<CongressTransactionStatusTransition> existing = _repository.Query().ToList().Where(entity => entity.CongressId == request.CongressId).ToList();
            HashSet<Guid> existingIds = existing.Select(entity => entity.Id).ToHashSet();
            HashSet<Guid> requestIds = request.Items.Select(item => item.Id).ToHashSet();

            if (!existingIds.SetEquals(requestIds))
                throw new BusinessException(CongressWorkflowsMessages.ReorderInvalid);

            foreach (ReorderCongressWorkflowTransitionItemDto item in request.Items.OrderBy(item => item.Order))
            {
                CongressTransactionStatusTransition entity = existing.First(x => x.Id == item.Id);
                entity.Order = item.Order;
                await _repository.UpdateAsync(entity);
            }

            return new ReorderedCongressWorkflowTransitionsResponse { CongressId = request.CongressId };
        }
    }
}

public class ReorderCongressWorkflowTransitionItemDto
{
    public Guid Id { get; set; }
    public int Order { get; set; }
}

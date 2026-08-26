using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.WorkflowTemplateTransitions.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.WorkflowTemplateTransitions.Commands.Reorder;

public class ReorderWorkflowTemplateTransitionCommand : IRequest<ReorderedWorkflowTemplateTransitionResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid WorkflowTemplateId { get; set; }
    public IList<ReorderWorkflowTemplateTransitionItemDto> Items { get; set; } = new List<ReorderWorkflowTemplateTransitionItemDto>();

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetWorkflowTemplates";
    public string[] Roles => new[] { WorkflowTemplateTransitionsOperationClaims.Admin, WorkflowTemplateTransitionsOperationClaims.Write, WorkflowTemplateTransitionsOperationClaims.Update };

    public class ReorderWorkflowTemplateTransitionCommandHandler : IRequestHandler<ReorderWorkflowTemplateTransitionCommand, ReorderedWorkflowTemplateTransitionResponse>
    {
        private readonly IWorkflowTemplateTransitionRepository _repository;

        public ReorderWorkflowTemplateTransitionCommandHandler(IWorkflowTemplateTransitionRepository repository)
        {
            _repository = repository;
        }

        public async Task<ReorderedWorkflowTemplateTransitionResponse> Handle(ReorderWorkflowTemplateTransitionCommand request, CancellationToken cancellationToken)
        {
            if (request.Items.Count == 0)
                throw new BusinessException(WorkflowTemplateTransitionsMessages.ReorderInvalid);

            List<WorkflowTemplateTransition> existing = _repository.Query().ToList().Where(entity => entity.WorkflowTemplateId == request.WorkflowTemplateId).ToList();
            HashSet<Guid> existingIds = existing.Select(entity => entity.Id).ToHashSet();
            HashSet<Guid> requestIds = request.Items.Select(item => item.Id).ToHashSet();

            if (!existingIds.SetEquals(requestIds))
                throw new BusinessException(WorkflowTemplateTransitionsMessages.ReorderInvalid);

            foreach (ReorderWorkflowTemplateTransitionItemDto item in request.Items.OrderBy(item => item.Order))
            {
                WorkflowTemplateTransition entity = existing.First(x => x.Id == item.Id);
                entity.Order = item.Order;
                await _repository.UpdateAsync(entity);
            }

            return new ReorderedWorkflowTemplateTransitionResponse { WorkflowTemplateId = request.WorkflowTemplateId };
        }
    }
}

public class ReorderWorkflowTemplateTransitionItemDto
{
    public Guid Id { get; set; }
    public int Order { get; set; }
}

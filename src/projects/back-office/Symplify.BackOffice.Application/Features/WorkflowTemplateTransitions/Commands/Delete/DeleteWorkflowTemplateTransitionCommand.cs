using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.WorkflowTemplateTransitions.Constants;
using Symplify.BackOffice.Application.Features.WorkflowTemplateTransitions.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.WorkflowTemplateTransitions.Commands.Delete;

public class DeleteWorkflowTemplateTransitionCommand : IRequest<DeletedWorkflowTemplateTransitionResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetWorkflowTemplates";
    public string[] Roles => new[] { WorkflowTemplateTransitionsOperationClaims.Admin, WorkflowTemplateTransitionsOperationClaims.Write, WorkflowTemplateTransitionsOperationClaims.Delete };

    public class DeleteWorkflowTemplateTransitionCommandHandler : IRequestHandler<DeleteWorkflowTemplateTransitionCommand, DeletedWorkflowTemplateTransitionResponse>
    {
        private readonly IWorkflowTemplateTransitionRepository _repository;
        private readonly WorkflowTemplateTransitionBusinessRules _rules;

        public DeleteWorkflowTemplateTransitionCommandHandler(IWorkflowTemplateTransitionRepository repository, WorkflowTemplateTransitionBusinessRules rules)
        {
            _repository = repository;
            _rules = rules;
        }

        public async Task<DeletedWorkflowTemplateTransitionResponse> Handle(DeleteWorkflowTemplateTransitionCommand request, CancellationToken cancellationToken)
        {
            WorkflowTemplateTransition? entity = await _repository.GetAsync(predicate: x => x.Id == request.Id, cancellationToken: cancellationToken);
            await _rules.WorkflowTemplateTransitionShouldExistWhenSelected(entity);
            await _rules.WorkflowTemplateTransitionShouldNotBeUsedByCongress(request.Id);
            Guid templateId = entity!.WorkflowTemplateId;
            await _repository.DeleteAsync(entity);
            await NormalizeOrdersAsync(templateId);
            return new DeletedWorkflowTemplateTransitionResponse { Id = request.Id };
        }

        private async Task NormalizeOrdersAsync(Guid templateId)
        {
            List<WorkflowTemplateTransition> items = _repository.Query().ToList().Where(entity => entity.WorkflowTemplateId == templateId).OrderBy(entity => entity.Order).ThenBy(entity => entity.Id).ToList();
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

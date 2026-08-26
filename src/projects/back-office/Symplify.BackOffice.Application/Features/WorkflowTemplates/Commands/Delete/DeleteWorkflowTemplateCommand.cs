using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.WorkflowTemplates.Constants;
using Symplify.BackOffice.Application.Features.WorkflowTemplates.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.WorkflowTemplates.Commands.Delete;

public class DeleteWorkflowTemplateCommand : IRequest<DeletedWorkflowTemplateResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetWorkflowTemplates";
    public string[] Roles => new[] { WorkflowTemplatesOperationClaims.Admin, WorkflowTemplatesOperationClaims.Write, WorkflowTemplatesOperationClaims.Delete };

    public class DeleteWorkflowTemplateCommandHandler : IRequestHandler<DeleteWorkflowTemplateCommand, DeletedWorkflowTemplateResponse>
    {
        private readonly IWorkflowTemplateRepository _repository;
        private readonly WorkflowTemplateBusinessRules _rules;

        public DeleteWorkflowTemplateCommandHandler(IWorkflowTemplateRepository repository, WorkflowTemplateBusinessRules rules)
        {
            _repository = repository;
            _rules = rules;
        }

        public async Task<DeletedWorkflowTemplateResponse> Handle(DeleteWorkflowTemplateCommand request, CancellationToken cancellationToken)
        {
            WorkflowTemplate? entity = await _repository.GetAsync(predicate: x => x.Id == request.Id, cancellationToken: cancellationToken);
            await _rules.WorkflowTemplateShouldExistWhenSelected(entity);
            await _rules.WorkflowTemplateShouldNotBeUsedByCongress(request.Id);
            await _repository.DeleteAsync(entity!);
            return new DeletedWorkflowTemplateResponse { Id = request.Id };
        }
    }
}

using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressWorkflows.Constants;
using Symplify.BackOffice.Application.Features.CongressWorkflows.Rules;
using Symplify.BackOffice.Application.Services.Workflow;

namespace Symplify.BackOffice.Application.Features.CongressWorkflows.Commands.ApplyTemplate;

public class ApplyWorkflowTemplateToCongressCommand : IRequest<AppliedWorkflowTemplateToCongressResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressId { get; set; }
    public Guid WorkflowTemplateId { get; set; }
    public bool ReplaceExistingTransitions { get; set; } = true;

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => $"GetCongressWorkflow({CongressId})";
    public string[] Roles => new[] { CongressWorkflowsOperationClaims.Admin, CongressWorkflowsOperationClaims.Write, CongressWorkflowsOperationClaims.Update };

    public class ApplyWorkflowTemplateToCongressCommandHandler : IRequestHandler<ApplyWorkflowTemplateToCongressCommand, AppliedWorkflowTemplateToCongressResponse>
    {
        private readonly CongressWorkflowBusinessRules _rules;
        private readonly IWorkflowTemplateCopyService _copyService;

        public ApplyWorkflowTemplateToCongressCommandHandler(CongressWorkflowBusinessRules rules, IWorkflowTemplateCopyService copyService)
        {
            _rules = rules;
            _copyService = copyService;
        }

        public async Task<AppliedWorkflowTemplateToCongressResponse> Handle(ApplyWorkflowTemplateToCongressCommand request, CancellationToken cancellationToken)
        {
            await _rules.CongressShouldExist(request.CongressId);
            await _rules.WorkflowTemplateShouldExistAndBeActive(request.WorkflowTemplateId);
            await _copyService.ApplyTemplateToCongressAsync(request.CongressId, request.WorkflowTemplateId, request.ReplaceExistingTransitions, cancellationToken);

            return new AppliedWorkflowTemplateToCongressResponse { CongressId = request.CongressId, WorkflowTemplateId = request.WorkflowTemplateId };
        }
    }
}

using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.Submissions.Constants;
using Symplify.BackOffice.Application.Services.Workflow;

namespace Symplify.BackOffice.Application.Features.SubmissionWorkflow.Queries.GetAllowedTransitions;

public sealed class GetAllowedSubmissionTransitionsQuery : IRequest<GetAllowedSubmissionTransitionsResponse>, ISecuredRequest
{
    public Guid SubmissionId { get; set; }

    public Guid? PerformedByUserId { get; set; }

    public string[] Roles => new[]
    {
        SubmissionsOperationClaims.Admin,
        SubmissionsOperationClaims.Read
    };

    public sealed class Handler : IRequestHandler<GetAllowedSubmissionTransitionsQuery, GetAllowedSubmissionTransitionsResponse>
    {
        private readonly IWorkflowEngine _workflowEngine;

        public Handler(IWorkflowEngine workflowEngine)
        {
            _workflowEngine = workflowEngine;
        }

        public async Task<GetAllowedSubmissionTransitionsResponse> Handle(
            GetAllowedSubmissionTransitionsQuery request,
            CancellationToken cancellationToken)
        {
            IReadOnlyCollection<AllowedWorkflowTransitionDto> transitions = await _workflowEngine.GetAllowedTransitionsAsync(
                request.SubmissionId,
                request.PerformedByUserId,
                cancellationToken);

            return new GetAllowedSubmissionTransitionsResponse
            {
                SubmissionId = request.SubmissionId,
                Transitions = transitions.ToList()
            };
        }
    }
}

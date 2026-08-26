using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.Submissions.Constants;
using Symplify.BackOffice.Application.Services.Workflow;

namespace Symplify.BackOffice.Application.Features.SubmissionWorkflow.Commands.ChangeStatus;

public sealed class ChangeSubmissionStatusCommand : IRequest<ChangedSubmissionStatusResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid SubmissionId { get; set; }

    public int TransitionId { get; set; }

    public Guid? PerformedByUserId { get; set; }

    public string? PublicNote { get; set; }

    public string? InternalNote { get; set; }

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetSubmissions";

    public string[] Roles => new[]
    {
        SubmissionsOperationClaims.Admin,
        SubmissionsOperationClaims.Write,
        SubmissionsOperationClaims.Update
    };

    public sealed class Handler : IRequestHandler<ChangeSubmissionStatusCommand, ChangedSubmissionStatusResponse>
    {
        private readonly IWorkflowEngine _workflowEngine;

        public Handler(IWorkflowEngine workflowEngine)
        {
            _workflowEngine = workflowEngine;
        }

        public async Task<ChangedSubmissionStatusResponse> Handle(
            ChangeSubmissionStatusCommand request,
            CancellationToken cancellationToken)
        {
            ChangeWorkflowStatusResult result = await _workflowEngine.ChangeStatusAsync(
                request.SubmissionId,
                request.TransitionId,
                request.PerformedByUserId,
                request.PublicNote,
                request.InternalNote,
                cancellationToken);

            return new ChangedSubmissionStatusResponse
            {
                Success = result.Success,
                Message = result.Message,
                SubmissionId = request.SubmissionId,
                NewStatusId = result.NewStatusId
            };
        }
    }
}

using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.Submissions.Constants;
using Symplify.BackOffice.Application.Services.Workflow;

namespace Symplify.BackOffice.Application.Features.SubmissionWorkflow.Commands.SetStatus;

public sealed class SetSubmissionWorkflowStatusCommand : IRequest<SetSubmissionWorkflowStatusResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid SubmissionId { get; set; }

    public string TargetStatusCode { get; set; } = string.Empty;

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

    public sealed class Handler : IRequestHandler<SetSubmissionWorkflowStatusCommand, SetSubmissionWorkflowStatusResponse>
    {
        private readonly IWorkflowEngine _workflowEngine;

        public Handler(IWorkflowEngine workflowEngine)
        {
            _workflowEngine = workflowEngine;
        }

        public async Task<SetSubmissionWorkflowStatusResponse> Handle(
            SetSubmissionWorkflowStatusCommand request,
            CancellationToken cancellationToken)
        {
            ChangeWorkflowStatusResult result = await _workflowEngine.ChangeStatusByCodeAsync(
                request.SubmissionId,
                request.TargetStatusCode,
                request.PerformedByUserId,
                request.PublicNote,
                request.InternalNote,
                cancellationToken);

            return new SetSubmissionWorkflowStatusResponse
            {
                Success = result.Success,
                Message = result.Message,
                SubmissionId = request.SubmissionId,
                NewStatusId = result.NewStatusId
            };
        }
    }
}

public sealed class SetSubmissionWorkflowStatusResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public Guid SubmissionId { get; init; }
    public int? NewStatusId { get; init; }
}

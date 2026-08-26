using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Features.Submissions.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Application.Services.Workflow;
using Symplify.BackOffice.Domain.Submission;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.SubmissionWorkflow.Commands.Reject;

public sealed class RejectSubmissionCommand : IRequest<RejectSubmissionResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid SubmissionId { get; set; }

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

    public sealed class Handler : IRequestHandler<RejectSubmissionCommand, RejectSubmissionResponse>
    {
        private const string AuditActor = "SubmissionRejected";

        private readonly ISubmissionRepository _submissionRepository;
        private readonly ITransactionStatusRepository _transactionStatusRepository;
        private readonly ISubmissionHistoryRepository _historyRepository;

        public Handler(
            ISubmissionRepository submissionRepository,
            ITransactionStatusRepository transactionStatusRepository,
            ISubmissionHistoryRepository historyRepository)
        {
            _submissionRepository = submissionRepository;
            _transactionStatusRepository = transactionStatusRepository;
            _historyRepository = historyRepository;
        }

        public async Task<RejectSubmissionResponse> Handle(
            RejectSubmissionCommand request,
            CancellationToken cancellationToken)
        {
            Submission? submission = await _submissionRepository
                .Query()
                .FirstOrDefaultAsync(item => item.Id == request.SubmissionId && item.DeletedDate == null, cancellationToken);

            if (submission is null)
                return RejectSubmissionResponse.Failed(request.SubmissionId, "Bildiri bulunamadı.");

            TransactionStatus? rejectedStatus = await _transactionStatusRepository
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(status =>
                    status.Code == SubmissionWorkflowStatusCodes.Rejected &&
                    status.DeletedDate == null,
                    cancellationToken);

            if (rejectedStatus is null)
                return RejectSubmissionResponse.Failed(request.SubmissionId, "Reddedildi workflow durumu bulunamadı.");

            if (submission.TransactionStatusId == rejectedStatus.Id)
                return RejectSubmissionResponse.Failed(request.SubmissionId, "Bildiri zaten reddedilmiş durumda.");

            DateTime now = DateTime.UtcNow;
            int? previousStatusId = submission.TransactionStatusId;

            submission.TransactionStatusId = rejectedStatus.Id;
            submission.PaymentStatusId = null;
            submission.UpdatedDate = now;
            submission.UpdatedBy = AuditActor;

            await _submissionRepository.UpdateAsync(submission);

            await _historyRepository.AddAsync(new SubmissionHistory
            {
                Id = Guid.NewGuid(),
                SubmissionId = submission.Id,
                FromStatusId = previousStatusId,
                ToStatusId = rejectedStatus.Id,
                TransactionStatusTransitionId = null,
                PerformedByUserId = request.PerformedByUserId,
                Note = string.IsNullOrWhiteSpace(request.PublicNote) ? "Bildiri reddedildi." : request.PublicNote,
                PublicNote = string.IsNullOrWhiteSpace(request.PublicNote) ? "Bildiriniz reddedildi." : request.PublicNote,
                InternalNote = string.IsNullOrWhiteSpace(request.InternalNote)
                    ? "Bildiri editör/yönetici tarafından manuel olarak reddedildi. Yazar erişimi kapatıldı; editör geçmişi korunur."
                    : request.InternalNote,
                PerformedAt = now,
                IsAutomatic = false,
                CreatedDate = now,
                CreatedBy = AuditActor
            });

            return RejectSubmissionResponse.Ok(submission.Id, rejectedStatus.Id);
        }
    }
}

public sealed class RejectSubmissionResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public Guid SubmissionId { get; init; }
    public int? NewStatusId { get; init; }

    public static RejectSubmissionResponse Ok(Guid submissionId, int? newStatusId)
        => new()
        {
            Success = true,
            SubmissionId = submissionId,
            NewStatusId = newStatusId
        };

    public static RejectSubmissionResponse Failed(Guid submissionId, string message)
        => new()
        {
            Success = false,
            SubmissionId = submissionId,
            Message = message
        };
}

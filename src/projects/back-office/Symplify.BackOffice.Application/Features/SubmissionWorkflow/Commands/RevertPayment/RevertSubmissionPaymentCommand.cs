using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Features.Submissions.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Application.Features.SubmissionWorkflow.Commands.RevertPayment;

public sealed class RevertSubmissionPaymentCommand : IRequest<RevertedSubmissionPaymentResponse>, ISecuredRequest, ICacheRemoverRequest
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

    public sealed class Handler : IRequestHandler<RevertSubmissionPaymentCommand, RevertedSubmissionPaymentResponse>
    {
        private readonly ISubmissionRepository _submissionRepository;
        private readonly IPaymentStatusRepository _paymentStatusRepository;
        private readonly ISubmissionHistoryRepository _historyRepository;

        public Handler(
            ISubmissionRepository submissionRepository,
            IPaymentStatusRepository paymentStatusRepository,
            ISubmissionHistoryRepository historyRepository)
        {
            _submissionRepository = submissionRepository;
            _paymentStatusRepository = paymentStatusRepository;
            _historyRepository = historyRepository;
        }

        public async Task<RevertedSubmissionPaymentResponse> Handle(
            RevertSubmissionPaymentCommand request,
            CancellationToken cancellationToken)
        {
            Submission? submission = await _submissionRepository.GetAsync(
                predicate: item => item.Id == request.SubmissionId,
                cancellationToken: cancellationToken);

            if (submission is null)
                return RevertedSubmissionPaymentResponse.Failed(request.SubmissionId, "Bildiri bulunamadı.");

            int? pendingStatusId = await ResolvePaymentStatusIdAsync(
                cancellationToken,
                "PAYMENT_PENDING",
                "PENDING",
                "WAITING_PAYMENT",
                "WAITING");

            if (!pendingStatusId.HasValue)
                return RevertedSubmissionPaymentResponse.Failed(request.SubmissionId, "Ödeme bekleniyor durumu bulunamadı.");

            int? completedStatusId = await ResolvePaymentStatusIdAsync(
                cancellationToken,
                "PAID",
                "PAYMENT_PAID",
                "PAYMENT_COMPLETED",
                "APPROVED",
                "PAYMENT_APPROVED",
                "COMPLETED");

            if (!completedStatusId.HasValue || submission.PaymentStatusId != completedStatusId.Value)
                return RevertedSubmissionPaymentResponse.Failed(request.SubmissionId, "Geri alınacak tamamlanmış ödeme işlemi bulunamadı.");

            submission.PaymentStatusId = pendingStatusId.Value;
            submission.UpdatedDate = DateTime.UtcNow;
            submission.UpdatedBy = "PaymentReverted";
            await _submissionRepository.UpdateAsync(submission);

            await AddPaymentRevertedHistoryAsync(submission, request, cancellationToken);

            return RevertedSubmissionPaymentResponse.Ok(request.SubmissionId, submission.TransactionStatusId, pendingStatusId.Value);
        }

        private async Task AddPaymentRevertedHistoryAsync(
            Submission submission,
            RevertSubmissionPaymentCommand request,
            CancellationToken cancellationToken)
        {
            SubmissionHistory history = new()
            {
                Id = Guid.NewGuid(),
                SubmissionId = submission.Id,
                FromStatusId = submission.TransactionStatusId,
                ToStatusId = submission.TransactionStatusId,
                TransactionStatusTransitionId = null,
                PerformedByUserId = request.PerformedByUserId,
                Note = string.IsNullOrWhiteSpace(request.PublicNote) ? "Ödeme işlemi geri alındı." : request.PublicNote,
                PublicNote = string.IsNullOrWhiteSpace(request.PublicNote) ? "Ödeme durumunuz yeniden bekleniyor olarak güncellendi." : request.PublicNote,
                InternalNote = string.IsNullOrWhiteSpace(request.InternalNote)
                    ? "Ödeme tamamlandı bilgisi editör/yönetici tarafından geri alındı. Bildiri kabul durumunda korunmuştur."
                    : request.InternalNote,
                PerformedAt = DateTime.UtcNow,
                IsAutomatic = false,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "PaymentReverted"
            };

            await _historyRepository.AddAsync(history);
        }

        private async Task<int?> ResolvePaymentStatusIdAsync(CancellationToken cancellationToken, params string[] codes)
        {
            foreach (string code in codes.Where(item => !string.IsNullOrWhiteSpace(item)))
            {
                string normalizedCode = code.Trim();
                var paymentStatus = await _paymentStatusRepository
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item =>
                        item.DeletedDate == null &&
                        item.IsActive &&
                        item.Code == normalizedCode,
                        cancellationToken);

                if (paymentStatus is not null)
                    return paymentStatus.Id;
            }

            return null;
        }
    }
}

public sealed class RevertedSubmissionPaymentResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public Guid SubmissionId { get; init; }
    public int? TransactionStatusId { get; init; }
    public int? PaymentStatusId { get; init; }

    public static RevertedSubmissionPaymentResponse Ok(Guid submissionId, int? transactionStatusId, int? paymentStatusId)
        => new()
        {
            Success = true,
            SubmissionId = submissionId,
            TransactionStatusId = transactionStatusId,
            PaymentStatusId = paymentStatusId
        };

    public static RevertedSubmissionPaymentResponse Failed(Guid submissionId, string message)
        => new()
        {
            Success = false,
            SubmissionId = submissionId,
            Message = message
        };
}

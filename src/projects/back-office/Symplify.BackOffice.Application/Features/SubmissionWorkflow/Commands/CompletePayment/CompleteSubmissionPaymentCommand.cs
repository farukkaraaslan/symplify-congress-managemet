using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Features.Submissions.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Application.Services.Workflow;
using Symplify.BackOffice.Domain.Submission;
using PaymentDocumentEntity = Symplify.BackOffice.Domain.Workflow.PaymentDocument;

namespace Symplify.BackOffice.Application.Features.SubmissionWorkflow.Commands.CompletePayment;

public sealed class CompleteSubmissionPaymentCommand : IRequest<CompletedSubmissionPaymentResponse>, ISecuredRequest, ICacheRemoverRequest
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

    public sealed class Handler : IRequestHandler<CompleteSubmissionPaymentCommand, CompletedSubmissionPaymentResponse>
    {
        private readonly ISubmissionRepository _submissionRepository;
        private readonly IPaymentStatusRepository _paymentStatusRepository;
        private readonly IPaymentDocumentRepository _paymentDocumentRepository;
        private readonly ISubmissionHistoryRepository _historyRepository;
        private readonly IMailOutboxService _mailOutboxService;

        public Handler(
            ISubmissionRepository submissionRepository,
            IPaymentStatusRepository paymentStatusRepository,
            IPaymentDocumentRepository paymentDocumentRepository,
            ISubmissionHistoryRepository historyRepository,
            IMailOutboxService mailOutboxService)
        {
            _submissionRepository = submissionRepository;
            _paymentStatusRepository = paymentStatusRepository;
            _paymentDocumentRepository = paymentDocumentRepository;
            _historyRepository = historyRepository;
            _mailOutboxService = mailOutboxService;
        }

        public async Task<CompletedSubmissionPaymentResponse> Handle(
            CompleteSubmissionPaymentCommand request,
            CancellationToken cancellationToken)
        {
            Submission? submission = await _submissionRepository.GetAsync(
                predicate: item => item.Id == request.SubmissionId,
                cancellationToken: cancellationToken);

            if (submission is null)
                return CompletedSubmissionPaymentResponse.Failed(request.SubmissionId, "Bildiri bulunamadı.");

            int? paidStatusId = await ResolvePaymentStatusIdAsync(
                cancellationToken,
                "PAID",
                "PAYMENT_PAID",
                "APPROVED",
                "PAYMENT_APPROVED",
                "COMPLETED");

            if (!paidStatusId.HasValue)
                return CompletedSubmissionPaymentResponse.Failed(request.SubmissionId, "Ödeme yapıldı durumu bulunamadı.");

            if (submission.PaymentStatusId != paidStatusId.Value)
            {
                submission.PaymentStatusId = paidStatusId.Value;
                submission.UpdatedDate = DateTime.UtcNow;
                submission.UpdatedBy = "PaymentCompleted";
                await _submissionRepository.UpdateAsync(submission);
            }

            await AddPaymentCompletedHistoryAsync(submission, request, cancellationToken);

            return CompletedSubmissionPaymentResponse.Ok(request.SubmissionId, submission.TransactionStatusId, paidStatusId.Value);
        }

        private async Task MarkPaymentDocumentsApprovedAsync(Guid submissionId, CancellationToken cancellationToken)
        {
            List<PaymentDocumentEntity> documents = await _paymentDocumentRepository
                .Query()
                .Where(document =>
                    document.SubmissionId == submissionId &&
                    document.DeletedDate == null &&
                    !document.IsApproved)
                .ToListAsync(cancellationToken);

            foreach (PaymentDocumentEntity document in documents)
            {
                document.IsApproved = true;
                document.UpdatedDate = DateTime.UtcNow;
                document.UpdatedBy = "PaymentCompleted";
                await _paymentDocumentRepository.UpdateAsync(document);
            }
        }

        private async Task AddPaymentCompletedHistoryAsync(
            Submission submission,
            CompleteSubmissionPaymentCommand request,
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
                Note = string.IsNullOrWhiteSpace(request.PublicNote) ? "Ödeme tamamlandı." : request.PublicNote,
                PublicNote = string.IsNullOrWhiteSpace(request.PublicNote) ? "Ödemeniz onaylandı." : request.PublicNote,
                InternalNote = string.IsNullOrWhiteSpace(request.InternalNote)
                    ? "Ödeme editör/yönetici tarafından manuel olarak tamamlandı. Bildiri kabul durumunda korunmuştur."
                    : request.InternalNote,
                PerformedAt = DateTime.UtcNow,
                IsAutomatic = false,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "PaymentCompleted"
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

public sealed class CompletedSubmissionPaymentResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public Guid SubmissionId { get; init; }
    public int? TransactionStatusId { get; init; }
    public int? PaymentStatusId { get; init; }

    public static CompletedSubmissionPaymentResponse Ok(Guid submissionId, int? transactionStatusId, int? paymentStatusId)
        => new()
        {
            Success = true,
            SubmissionId = submissionId,
            TransactionStatusId = transactionStatusId,
            PaymentStatusId = paymentStatusId
        };

    public static CompletedSubmissionPaymentResponse Failed(Guid submissionId, string message)
        => new()
        {
            Success = false,
            SubmissionId = submissionId,
            Message = message
        };
}

using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Features.Submissions.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Application.Services.Workflow;
using Symplify.BackOffice.Domain.Submission;
using PaymentDocumentEntity = Symplify.BackOffice.Domain.Workflow.PaymentDocument;
using SubmissionFileKind = Symplify.BackOffice.Domain.Enums.SubmissionFileKind;
using WorkflowTransactionStatus = Symplify.BackOffice.Domain.Workflow.TransactionStatus;

namespace Symplify.BackOffice.Application.Features.SubmissionWorkflow.Commands.RestartRejectedProcess;

public sealed class RestartRejectedSubmissionProcessCommand : IRequest<RestartRejectedSubmissionProcessResponse>, ISecuredRequest, ICacheRemoverRequest
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

    public sealed class Handler : IRequestHandler<RestartRejectedSubmissionProcessCommand, RestartRejectedSubmissionProcessResponse>
    {
        private const string AuditActor = "SubmissionProcessRestarted";

        private readonly ISubmissionRepository _submissionRepository;
        private readonly ITransactionStatusRepository _transactionStatusRepository;
        private readonly ISubmissionEvaluationRepository _submissionEvaluationRepository;
        private readonly IEvaluationScoreRepository _evaluationScoreRepository;
        private readonly ISubmissionAcceptanceLetterRepository _acceptanceLetterRepository;
        private readonly ISubmissionFileRepository _submissionFileRepository;
        private readonly IPaymentDocumentRepository _paymentDocumentRepository;
        private readonly ISubmissionHistoryRepository _historyRepository;

        public Handler(
            ISubmissionRepository submissionRepository,
            ITransactionStatusRepository transactionStatusRepository,
            ISubmissionEvaluationRepository submissionEvaluationRepository,
            IEvaluationScoreRepository evaluationScoreRepository,
            ISubmissionAcceptanceLetterRepository acceptanceLetterRepository,
            ISubmissionFileRepository submissionFileRepository,
            IPaymentDocumentRepository paymentDocumentRepository,
            ISubmissionHistoryRepository historyRepository)
        {
            _submissionRepository = submissionRepository;
            _transactionStatusRepository = transactionStatusRepository;
            _submissionEvaluationRepository = submissionEvaluationRepository;
            _evaluationScoreRepository = evaluationScoreRepository;
            _acceptanceLetterRepository = acceptanceLetterRepository;
            _submissionFileRepository = submissionFileRepository;
            _paymentDocumentRepository = paymentDocumentRepository;
            _historyRepository = historyRepository;
        }

        public async Task<RestartRejectedSubmissionProcessResponse> Handle(
            RestartRejectedSubmissionProcessCommand request,
            CancellationToken cancellationToken)
        {
            Submission? submission = await _submissionRepository
                .Query()
                .Include(item => item.Reviewers)
                .FirstOrDefaultAsync(item => item.Id == request.SubmissionId && item.DeletedDate == null, cancellationToken);

            if (submission is null)
                return RestartRejectedSubmissionProcessResponse.Failed(request.SubmissionId, "Bildiri bulunamadı.");

            WorkflowTransactionStatus? rejectedStatus = await ResolveStatusAsync(SubmissionWorkflowStatusCodes.Rejected, cancellationToken);
            if (rejectedStatus is null)
                return RestartRejectedSubmissionProcessResponse.Failed(request.SubmissionId, "Reddedildi workflow durumu bulunamadı.");

            WorkflowTransactionStatus? submittedStatus = await ResolveStatusAsync(SubmissionWorkflowStatusCodes.Submitted, cancellationToken);
            if (submittedStatus is null)
                return RestartRejectedSubmissionProcessResponse.Failed(request.SubmissionId, "Gönderildi workflow durumu bulunamadı.");

            if (submission.TransactionStatusId != rejectedStatus.Id)
                return RestartRejectedSubmissionProcessResponse.Failed(request.SubmissionId, "Süreç yalnızca reddedilmiş bildiriler için yeniden başlatılabilir.");

            DateTime now = DateTime.UtcNow;
            int? previousStatusId = submission.TransactionStatusId;

            await SoftDeleteEvaluationsAsync(submission.Id, now, cancellationToken);
            await SoftDeleteAcceptanceLettersAsync(submission.Id, now, cancellationToken);
            await SoftDeletePaymentDocumentsAsync(submission.Id, now, cancellationToken);

            submission.Reviewers.Clear();
            submission.TransactionStatusId = submittedStatus.Id;
            submission.PaymentStatusId = null;
            submission.IsSubmitted = true;
            submission.SubmittedAt = now;
            submission.UpdatedDate = now;
            submission.UpdatedBy = AuditActor;

            await _submissionRepository.UpdateAsync(submission);

            await _historyRepository.AddAsync(new SubmissionHistory
            {
                Id = Guid.NewGuid(),
                SubmissionId = submission.Id,
                FromStatusId = previousStatusId,
                ToStatusId = submittedStatus.Id,
                TransactionStatusTransitionId = null,
                PerformedByUserId = request.PerformedByUserId,
                Note = string.IsNullOrWhiteSpace(request.PublicNote) ? "Bildiri süreci yeniden başlatıldı." : request.PublicNote,
                PublicNote = string.IsNullOrWhiteSpace(request.PublicNote) ? "Bildiri süreci yeniden başlatıldı." : request.PublicNote,
                InternalNote = string.IsNullOrWhiteSpace(request.InternalNote)
                    ? "Reddedilmiş bildiri yeniden gönderildi durumuna alındı. Hakem atamaları, değerlendirmeler, kabul belgeleri ve ödeme süreci yeni süreç için sıfırlandı."
                    : request.InternalNote,
                PerformedAt = now,
                IsAutomatic = false,
                CreatedDate = now,
                CreatedBy = AuditActor
            });

            return RestartRejectedSubmissionProcessResponse.Ok(submission.Id, submittedStatus.Id);
        }

        private async Task<WorkflowTransactionStatus?> ResolveStatusAsync(string code, CancellationToken cancellationToken)
        {
            return await _transactionStatusRepository
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(status =>
                    status.Code == code &&
                    status.DeletedDate == null,
                    cancellationToken);
        }

        private async Task SoftDeleteEvaluationsAsync(Guid submissionId, DateTime now, CancellationToken cancellationToken)
        {
            List<SubmissionEvaluation> evaluations = await _submissionEvaluationRepository
                .Query()
                .Where(evaluation => evaluation.SubmissionId == submissionId && evaluation.DeletedDate == null)
                .ToListAsync(cancellationToken);

            if (evaluations.Count == 0)
                return;

            List<Guid> evaluationIds = evaluations.Select(evaluation => evaluation.Id).ToList();

            List<EvaluationScore> scores = await _evaluationScoreRepository
                .Query()
                .Where(score => evaluationIds.Contains(score.SubmissionEvaluationId) && score.DeletedDate == null)
                .ToListAsync(cancellationToken);

            foreach (EvaluationScore score in scores)
            {
                score.DeletedDate = now;
                score.DeletedBy = AuditActor;
                score.UpdatedDate = now;
                score.UpdatedBy = AuditActor;
                await _evaluationScoreRepository.UpdateAsync(score);
            }

            foreach (SubmissionEvaluation evaluation in evaluations)
            {
                evaluation.DeletedDate = now;
                evaluation.DeletedBy = AuditActor;
                evaluation.UpdatedDate = now;
                evaluation.UpdatedBy = AuditActor;
                await _submissionEvaluationRepository.UpdateAsync(evaluation);
            }
        }

        private async Task SoftDeleteAcceptanceLettersAsync(Guid submissionId, DateTime now, CancellationToken cancellationToken)
        {
            List<SubmissionAcceptanceLetter> letters = await _acceptanceLetterRepository
                .Query()
                .Where(letter => letter.SubmissionId == submissionId && letter.DeletedDate == null)
                .ToListAsync(cancellationToken);

            foreach (SubmissionAcceptanceLetter letter in letters)
            {
                letter.DeletedDate = now;
                letter.DeletedBy = AuditActor;
                letter.UpdatedDate = now;
                letter.UpdatedBy = AuditActor;
                await _acceptanceLetterRepository.UpdateAsync(letter);
            }

            List<SubmissionFile> acceptanceFiles = await _submissionFileRepository
                .Query()
                .Where(file =>
                    file.SubmissionId == submissionId &&
                    file.FileKind == SubmissionFileKind.AcceptanceLetter &&
                    file.DeletedDate == null)
                .ToListAsync(cancellationToken);

            foreach (SubmissionFile file in acceptanceFiles)
            {
                file.IsActive = false;
                file.DeletedDate = now;
                file.DeletedBy = AuditActor;
                file.UpdatedDate = now;
                file.UpdatedBy = AuditActor;
                await _submissionFileRepository.UpdateAsync(file);
            }
        }

        private async Task SoftDeletePaymentDocumentsAsync(Guid submissionId, DateTime now, CancellationToken cancellationToken)
        {
            List<PaymentDocumentEntity> documents = await _paymentDocumentRepository
                .Query()
                .Where(document => document.SubmissionId == submissionId && document.DeletedDate == null)
                .ToListAsync(cancellationToken);

            foreach (PaymentDocumentEntity document in documents)
            {
                document.DeletedDate = now;
                document.DeletedBy = AuditActor;
                document.UpdatedDate = now;
                document.UpdatedBy = AuditActor;
                await _paymentDocumentRepository.UpdateAsync(document);
            }
        }
    }
}

public sealed class RestartRejectedSubmissionProcessResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public Guid SubmissionId { get; init; }
    public int? NewStatusId { get; init; }

    public static RestartRejectedSubmissionProcessResponse Ok(Guid submissionId, int? newStatusId)
        => new()
        {
            Success = true,
            SubmissionId = submissionId,
            NewStatusId = newStatusId
        };

    public static RestartRejectedSubmissionProcessResponse Failed(Guid submissionId, string message)
        => new()
        {
            Success = false,
            SubmissionId = submissionId,
            Message = message
        };
}

using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Features.SubmissionReviewers.Constants;
using Symplify.BackOffice.Application.Features.Submissions.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Application.Services.Workflow;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Submission;
using Symplify.BackOffice.Domain.Workflow;
using TransactionStatus = Symplify.BackOffice.Domain.Workflow.TransactionStatus;

namespace Symplify.BackOffice.Application.Features.SubmissionReviewers.Commands.Assign;

public sealed class AssignReviewerToSubmissionCommand : IRequest<AssignedReviewerToSubmissionResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid SubmissionId { get; set; }
    public Guid ReviewerId { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Note { get; set; }

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetSubmissions";

    public string[] Roles => new[]
    {
        SubmissionsOperationClaims.Admin,
        SubmissionsOperationClaims.Write,
        SubmissionsOperationClaims.Update
    };

    public sealed class Handler : IRequestHandler<AssignReviewerToSubmissionCommand, AssignedReviewerToSubmissionResponse>
    {
        private readonly ISubmissionRepository _submissionRepository;
        private readonly IReviewerRepository _reviewerRepository;
        private readonly ICongressReviewerRepository _congressReviewerRepository;
        private readonly ISubmissionEvaluationRepository _submissionEvaluationRepository;
        private readonly ISubmissionHistoryRepository _submissionHistoryRepository;
        private readonly ITransactionStatusRepository _transactionStatusRepository;
        private readonly ITransactionStatusTransitionRepository _transactionStatusTransitionRepository;
        private readonly IWorkflowEngine _workflowEngine;

        private const string SubmittedStatusCode = "SUBMITTED";
        private const string ReviewerAssignmentStatusCode = "REVIEWER_ASSIGNMENT";
        private const string UnderReviewStatusCode = "UNDER_REVIEW";

        public Handler(
            ISubmissionRepository submissionRepository,
            IReviewerRepository reviewerRepository,
            ICongressReviewerRepository congressReviewerRepository,
            ISubmissionEvaluationRepository submissionEvaluationRepository,
            ISubmissionHistoryRepository submissionHistoryRepository,
            ITransactionStatusRepository transactionStatusRepository,
            ITransactionStatusTransitionRepository transactionStatusTransitionRepository,
            IWorkflowEngine workflowEngine)
        {
            _submissionRepository = submissionRepository;
            _reviewerRepository = reviewerRepository;
            _congressReviewerRepository = congressReviewerRepository;
            _submissionEvaluationRepository = submissionEvaluationRepository;
            _submissionHistoryRepository = submissionHistoryRepository;
            _transactionStatusRepository = transactionStatusRepository;
            _transactionStatusTransitionRepository = transactionStatusTransitionRepository;
            _workflowEngine = workflowEngine;
        }

        public async Task<AssignedReviewerToSubmissionResponse> Handle(AssignReviewerToSubmissionCommand request, CancellationToken cancellationToken)
        {
            Submission? submission = await _submissionRepository
                .Query()
                .Include(item => item.Reviewers)
                .FirstOrDefaultAsync(item => item.Id == request.SubmissionId && item.DeletedDate == null, cancellationToken);

            if (submission is null)
                throw new BusinessException(SubmissionReviewerMessages.SubmissionNotFound);

            if (submission.TransactionStatusId is null)
                throw new BusinessException(SubmissionReviewerMessages.WorkflowStatusNotFound);

            bool hasCompletedEvaluation = await _submissionEvaluationRepository
                .Query()
                .AsNoTracking()
                .AnyAsync(item =>
                    item.SubmissionId == submission.Id &&
                    item.CompletedAt.HasValue &&
                    item.DeletedDate == null,
                    cancellationToken);

            if (hasCompletedEvaluation)
                throw new BusinessException(SubmissionReviewerMessages.EvaluationCompletedNoNewReviewer);

            Reviewer? reviewer = await _reviewerRepository
                .Query()
                .Include(item => item.User)
                .FirstOrDefaultAsync(item => item.Id == request.ReviewerId && item.DeletedDate == null, cancellationToken);

            if (reviewer is null)
                throw new BusinessException(SubmissionReviewerMessages.ReviewerNotFound);

            if (!reviewer.IsActive || reviewer.Status == ReviewerStatus.Declined)
                throw new BusinessException(SubmissionReviewerMessages.ReviewerNotActive);

            bool alreadyAssigned = submission.Reviewers.Any(item => item.Id == reviewer.Id && item.DeletedDate == null);
            if (alreadyAssigned)
                throw new BusinessException(SubmissionReviewerMessages.ReviewerAlreadyAssigned);

            CongressReviewer? congressReviewer = await _congressReviewerRepository
                .Query()
                .FirstOrDefaultAsync(item => item.CongressId == submission.CongressId && item.ReviewerId == reviewer.Id && item.DeletedDate == null, cancellationToken);

            if (congressReviewer is null)
            {
                congressReviewer = new CongressReviewer
                {
                    Id = Guid.NewGuid(),
                    CongressId = submission.CongressId,
                    ReviewerId = reviewer.Id,
                    IsActive = true,
                    AcceptedAt = DateTime.UtcNow,
                    Note = "Hakem, bildiri ataması sırasında kongre hakem havuzuna otomatik eklendi."
                };

                await _congressReviewerRepository.AddAsync(congressReviewer);
            }
            else if (!congressReviewer.IsActive)
            {
                throw new BusinessException(SubmissionReviewerMessages.ReviewerPassiveInCongress);
            }

            string reviewerName = JoinFullName(reviewer.User?.Name, reviewer.User?.Surname);
            string historyNote = request.DueDate.HasValue
                ? $"Hakem atandı: {reviewerName}. Son değerlendirme tarihi: {request.DueDate.Value:dd.MM.yyyy}."
                : $"Hakem atandı: {reviewerName}.";

            if (!string.IsNullOrWhiteSpace(request.Note))
                historyNote = $"{historyNote} Not: {request.Note}";

            bool workflowTransitionApplied = await MoveSubmissionToReviewerAssignmentAsync(
                submission,
                request.PerformedByUserId,
                historyNote,
                request.Note,
                cancellationToken);

            submission.Reviewers.Add(reviewer);

            bool hasEvaluation = await _submissionEvaluationRepository
                .Query()
                .AnyAsync(item => item.SubmissionId == submission.Id && item.ReviewerId == reviewer.Id && item.DeletedDate == null, cancellationToken);

            if (!hasEvaluation)
            {
                await _submissionEvaluationRepository.AddAsync(new SubmissionEvaluation
                {
                    Id = Guid.NewGuid(),
                    SubmissionId = submission.Id,
                    ReviewerId = reviewer.Id
                });
            }

            await _submissionRepository.UpdateAsync(submission);

            if (!workflowTransitionApplied)
            {
                await _submissionHistoryRepository.AddAsync(new SubmissionHistory
                {
                    Id = Guid.NewGuid(),
                    SubmissionId = submission.Id,
                    FromStatusId = null,
                    ToStatusId = null,
                    PerformedByUserId = request.PerformedByUserId,
                    Note = historyNote,
                    InternalNote = request.Note,
                    PerformedAt = DateTime.UtcNow,
                    IsAutomatic = false
                });
            }

            return new AssignedReviewerToSubmissionResponse
            {
                SubmissionId = submission.Id,
                ReviewerId = reviewer.Id,
                ReviewerName = reviewerName
            };
        }

        private async Task<bool> MoveSubmissionToReviewerAssignmentAsync(
            Submission submission,
            Guid? performedByUserId,
            string historyNote,
            string? internalNote,
            CancellationToken cancellationToken)
        {
            if (!submission.TransactionStatusId.HasValue)
                return false;

            TransactionStatus? currentStatus = await _transactionStatusRepository.GetAsync(
                predicate: status => status.Id == submission.TransactionStatusId.Value && status.DeletedDate == null,
                cancellationToken: cancellationToken);

            if (currentStatus is null)
                throw new BusinessException(SubmissionReviewerMessages.CurrentWorkflowStatusNotFound);

            if (currentStatus.Code == ReviewerAssignmentStatusCode || currentStatus.Code == UnderReviewStatusCode)
                return false;

            if (currentStatus.IsFinal)
                throw new BusinessException(SubmissionReviewerMessages.FinalStatusNoReviewer);

            if (!string.Equals(currentStatus.Code, SubmittedStatusCode, StringComparison.OrdinalIgnoreCase))
                return false;

            TransactionStatus? reviewerAssignmentStatus = await _transactionStatusRepository.GetAsync(
                predicate: status => status.Code == ReviewerAssignmentStatusCode && status.DeletedDate == null,
                cancellationToken: cancellationToken);

            if (reviewerAssignmentStatus is null)
                throw new BusinessException(SubmissionReviewerMessages.ReviewerAssignmentStatusNotFound);

            TransactionStatusTransition? transition = await _transactionStatusTransitionRepository.GetAsync(
                predicate: item =>
                    item.FromStatusId == currentStatus.Id &&
                    item.ToStatusId == reviewerAssignmentStatus.Id &&
                    item.IsActive &&
                    !item.IsAuto &&
                    item.DeletedDate == null,
                cancellationToken: cancellationToken);

            if (transition is null)
                throw new BusinessException(SubmissionReviewerMessages.ReviewerAssignmentTransitionNotFound);

            ChangeWorkflowStatusResult workflowResult = await _workflowEngine.ChangeStatusAsync(
                submission.Id,
                transition.Id,
                performedByUserId,
                historyNote,
                internalNote,
                cancellationToken);

            if (!workflowResult.Success)
                throw new BusinessException(workflowResult.Message ?? SubmissionReviewerMessages.ReviewerAssignmentWorkflowFailed);

            if (workflowResult.NewStatusId.HasValue)
                submission.TransactionStatusId = workflowResult.NewStatusId.Value;

            return true;
        }

        private static string JoinFullName(string? firstName, string? lastName)
        {
            string fullName = $"{firstName} {lastName}".Trim();
            return string.IsNullOrWhiteSpace(fullName) ? "-" : fullName;
        }
    }
}

using Core.Persistence.Paging;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Submission;
using Symplify.BackOffice.Domain.Workflow;
using PaymentDocumentEntity = Symplify.BackOffice.Domain.Workflow.PaymentDocument;

namespace Symplify.BackOffice.Application.Services.Workflow;

public sealed class WorkflowConditionEvaluator : IWorkflowConditionEvaluator
{
    private readonly IReviewerRepository _reviewerRepository;
    private readonly ISubmissionEvaluationRepository _submissionEvaluationRepository;
    private readonly IPaymentDocumentRepository _paymentDocumentRepository;
    private readonly ISubmissionFileRepository _submissionFileRepository;

    public WorkflowConditionEvaluator(
        IReviewerRepository reviewerRepository,
        ISubmissionEvaluationRepository submissionEvaluationRepository,
        IPaymentDocumentRepository paymentDocumentRepository,
        ISubmissionFileRepository submissionFileRepository)
    {
        _reviewerRepository = reviewerRepository;
        _submissionEvaluationRepository = submissionEvaluationRepository;
        _paymentDocumentRepository = paymentDocumentRepository;
        _submissionFileRepository = submissionFileRepository;
    }

    public async Task<WorkflowConditionEvaluationResult> EvaluateAsync(
        WorkflowContext context,
        IReadOnlyCollection<WorkflowTransitionCondition> conditions,
        CancellationToken cancellationToken)
    {
        foreach (WorkflowTransitionCondition condition in conditions.OrderBy(item => item.Order))
        {
            decimal actualValue = await ResolveActualValueAsync(context, condition, cancellationToken);
            decimal expectedValue = await ResolveExpectedValueAsync(context, condition, cancellationToken);

            if (!Compare(actualValue, expectedValue, condition.Operator))
                return WorkflowConditionEvaluationResult.Denied(condition.FailureMessageResourceKey);
        }

        return WorkflowConditionEvaluationResult.Allowed();
    }

    private async Task<decimal> ResolveActualValueAsync(
        WorkflowContext context,
        WorkflowTransitionCondition condition,
        CancellationToken cancellationToken)
    {
        Guid submissionId = context.Submission.Id;

        if (condition.Subject == WorkflowConditionSubject.ReviewerAssignment && condition.Field == WorkflowConditionField.Count)
        {
            IPaginate<Reviewer> reviewers = await _reviewerRepository.GetListAsync(
                predicate: reviewer => reviewer.Submissions.Any(submission => submission.Id == submissionId),
                index: 0,
                size: 1000,
                cancellationToken: cancellationToken);

            return reviewers.Count;
        }

        if (condition.Subject == WorkflowConditionSubject.SubmissionEvaluation && condition.Field == WorkflowConditionField.CompletedCount)
        {
            IPaginate<SubmissionEvaluation> evaluations = await _submissionEvaluationRepository.GetListAsync(
                predicate: evaluation => evaluation.SubmissionId == submissionId && evaluation.CompletedAt != null,
                index: 0,
                size: 1000,
                cancellationToken: cancellationToken);

            return evaluations.Count;
        }

        if (condition.Subject == WorkflowConditionSubject.PaymentDocument && condition.Field == WorkflowConditionField.IsApproved)
        {
            IPaginate<PaymentDocumentEntity> documents = await _paymentDocumentRepository.GetListAsync(
                predicate: document => document.SubmissionId == submissionId && document.IsApproved,
                index: 0,
                size: 1,
                cancellationToken: cancellationToken);

            return documents.Count > 0 ? 1 : 0;
        }

        if (condition.Subject == WorkflowConditionSubject.SubmissionFile && condition.Field == WorkflowConditionField.Exists)
        {
            IPaginate<SubmissionFile> files = await _submissionFileRepository.GetListAsync(
                predicate: file => file.SubmissionId == submissionId && file.IsActive,
                index: 0,
                size: 1,
                cancellationToken: cancellationToken);

            return files.Count > 0 ? 1 : 0;
        }

        return 0;
    }

    private async Task<decimal> ResolveExpectedValueAsync(
        WorkflowContext context,
        WorkflowTransitionCondition condition,
        CancellationToken cancellationToken)
    {
        if (string.Equals(condition.ExpectedValueSource, "AssignedReviewerCount", StringComparison.OrdinalIgnoreCase))
        {
            IPaginate<Reviewer> reviewers = await _reviewerRepository.GetListAsync(
                predicate: reviewer => reviewer.Submissions.Any(submission => submission.Id == context.Submission.Id),
                index: 0,
                size: 1000,
                cancellationToken: cancellationToken);

            return reviewers.Count;
        }

        if (decimal.TryParse(condition.ExpectedValue, out decimal parsed))
            return parsed;

        if (bool.TryParse(condition.ExpectedValue, out bool parsedBool))
            return parsedBool ? 1 : 0;

        return 0;
    }

    private static bool Compare(decimal actual, decimal expected, WorkflowConditionOperator @operator)
    {
        return @operator switch
        {
            WorkflowConditionOperator.Equals => actual == expected,
            WorkflowConditionOperator.NotEquals => actual != expected,
            WorkflowConditionOperator.Exists => actual > 0,
            WorkflowConditionOperator.NotExists => actual <= 0,
            WorkflowConditionOperator.GreaterThan => actual > expected,
            WorkflowConditionOperator.GreaterThanOrEqual => actual >= expected,
            WorkflowConditionOperator.LessThan => actual < expected,
            WorkflowConditionOperator.LessThanOrEqual => actual <= expected,
            _ => false
        };
    }
}

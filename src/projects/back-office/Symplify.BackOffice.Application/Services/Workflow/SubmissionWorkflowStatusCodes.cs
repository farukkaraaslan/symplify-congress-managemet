namespace Symplify.BackOffice.Application.Services.Workflow;

public static class SubmissionWorkflowStatusCodes
{
    public const string Draft = "DRAFT";
    public const string Submitted = "SUBMITTED";
    public const string ReviewerAssignment = "REVIEWER_ASSIGNMENT";
    public const string UnderReview = "UNDER_REVIEW";
    public const string ReviewsCompleted = "REVIEWS_COMPLETED";
    public const string EditorialDecision = "EDITORIAL_DECISION";
    public const string RevisionRequested = "REVISION_REQUESTED";
    public const string Accepted = "ACCEPTED";
    public const string Rejected = "REJECTED";
    public const string PaymentPending = "PAYMENT_PENDING";
    public const string Completed = "COMPLETED";
    public const string Withdrawn = "WITHDRAWN";

    private static readonly HashSet<string> ReviewerInternalStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        ReviewerAssignment,
        UnderReview,
        ReviewsCompleted
    };

    public static bool IsReviewerInternalStatus(string? code)
    {
        return !string.IsNullOrWhiteSpace(code) && ReviewerInternalStatuses.Contains(code);
    }
}

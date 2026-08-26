namespace Symplify.BackOffice.Application.Features.Submissions.Constants;

public static class SubmissionManagementResourceKeys
{
    // ============================================================
    // Page / Breadcrumb
    // ============================================================

    public const string PageTitle = "BackOffice.Submissions.Management.Editor.PageTitle";
    public const string PageSubtitle = "BackOffice.Submissions.Management.Editor.PageSubtitle";
    public const string BreadcrumbDashboard = "BackOffice.Submissions.Management.Editor.Breadcrumb.Dashboard";
    public const string BreadcrumbIndex = "BackOffice.Submissions.Management.Editor.Breadcrumb.Index";
    public const string BreadcrumbDetail = "BackOffice.Submissions.Management.Editor.Breadcrumb.Detail";

    // ============================================================
    // Index / Info / Stats
    // ============================================================

    public const string IndexInfoTitle = "BackOffice.Submissions.Management.Editor.Index.InfoTitle";
    public const string IndexInfoDescription = "BackOffice.Submissions.Management.Editor.Index.InfoDescription";

    public const string IndexBadgeTitle = "BackOffice.Submissions.Management.Editor.Index.BadgeTitle";
    public const string ApplyFilters = "BackOffice.Submissions.Management.Editor.Filters.Apply";

    public const string StatTotal = "BackOffice.Submissions.Management.Editor.Stat.Total";
    public const string StatSubmitted = "BackOffice.Submissions.Management.Editor.Stat.Submitted";
    public const string StatReviewerProcess = "BackOffice.Submissions.Management.Editor.Stat.ReviewerProcess";
    public const string StatAccepted = "BackOffice.Submissions.Management.Editor.Stat.Accepted";
    public const string StatInReview = "BackOffice.Submissions.Management.Editor.Stat.InReview";
    public const string StatRevision = "BackOffice.Submissions.Management.Editor.Stat.Revision";
    public const string StatRejected = "BackOffice.Submissions.Management.Editor.Stat.Rejected";
    public const string StatPayment = "BackOffice.Submissions.Management.Editor.Stat.Payment";
    public const string StatPaymentPending = "BackOffice.Submissions.Management.Editor.Stat.PaymentPending";
    public const string StatPaymentCompleted = "BackOffice.Submissions.Management.Editor.Stat.PaymentCompleted";

    // ============================================================
    // Filters
    // ============================================================

    public const string FiltersTitle = "BackOffice.Submissions.Management.Editor.Filters.Title";
    public const string FiltersDescription = "BackOffice.Submissions.Management.Editor.Filters.Description";
    public const string ResetFilters = "BackOffice.Submissions.Management.Editor.Filters.Reset";

    public const string SearchLabel = "BackOffice.Submissions.Management.Editor.Search.Label";
    public const string SearchPlaceholder = "BackOffice.Submissions.Management.Editor.Search.Placeholder";
    public const string SearchButton = "BackOffice.Submissions.Management.Editor.Search.Button";

    public const string FilterCongressLabel = "BackOffice.Submissions.Management.Editor.Filters.Congress.Label";
    public const string FilterAllActiveCongresses = "BackOffice.Submissions.Management.Editor.Filters.Congress.AllActive";
    public const string FilterAllArchivedCongresses = "BackOffice.Submissions.Management.Editor.Filters.Congress.AllArchived";
    public const string ViewArchive = "BackOffice.Submissions.Management.Editor.Filters.ViewArchive";
    public const string ViewActive = "BackOffice.Submissions.Management.Editor.Filters.ViewActive";
    public const string FilterStatusLabel = "BackOffice.Submissions.Management.Editor.Filters.Status.Label";
    public const string FilterAllStatuses = "BackOffice.Submissions.Management.Editor.Filters.Status.All";
    public const string FilterPaymentLabel = "BackOffice.Submissions.Management.Editor.Filters.Payment.Label";
    public const string FilterAllPaymentStatuses = "BackOffice.Submissions.Management.Editor.Filters.Payment.All";
    public const string FilterTopicLabel = "BackOffice.Submissions.Management.Editor.Filters.Topic.Label";
    public const string FilterAllTopics = "BackOffice.Submissions.Management.Editor.Filters.Topic.All";
    public const string FilterSubmissionTypeLabel = "BackOffice.Submissions.Management.Editor.Filters.SubmissionType.Label";
    public const string FilterAllSubmissionTypes = "BackOffice.Submissions.Management.Editor.Filters.SubmissionType.All";

    public const string FilterOwnerMultiplicityLabel = "BackOffice.Submissions.Management.Editor.Filters.OwnerMultiplicity.Label";
    public const string FilterOwnerMultiplicityAll = "BackOffice.Submissions.Management.Editor.Filters.OwnerMultiplicity.All";
    public const string FilterOwnerMultiplicitySingle = "BackOffice.Submissions.Management.Editor.Filters.OwnerMultiplicity.Single";
    public const string FilterOwnerMultiplicityMultiple = "BackOffice.Submissions.Management.Editor.Filters.OwnerMultiplicity.Multiple";

    // ============================================================
    // List / Table
    // ============================================================

    public const string ListTitle = "BackOffice.Submissions.Management.Editor.List.Title";
    public const string ListDescription = "BackOffice.Submissions.Management.Editor.List.Description";
    public const string ExportExcel = "BackOffice.Submissions.Management.Editor.Action.ExportExcel";
    public const string EmptyTitle = "BackOffice.Submissions.Management.Editor.Empty.Title";
    public const string EmptyDescription = "BackOffice.Submissions.Management.Editor.Empty.Description";

    public const string ColumnManage = "BackOffice.Submissions.Management.Editor.Table.Manage";
    public const string ColumnRowNumber = "BackOffice.Submissions.Management.Column.RowNumber";
    public const string ColumnSubmission = "BackOffice.Submissions.Management.Editor.Table.Submission";

    public const string ColumnSubmissionNumber = "BackOffice.Submissions.Management.Editor.Table.SubmissionNumber";
    public const string ColumnSubmissionTitle = "BackOffice.Submissions.Management.Editor.Table.SubmissionTitle";
    public const string ColumnCongress = "BackOffice.Submissions.Management.Editor.Table.Congress";
    public const string ColumnTypeTopic = "BackOffice.Submissions.Management.Editor.Table.TypeTopic";
    public const string ColumnSubmissionOwner = "BackOffice.Submissions.Management.Editor.Table.SubmissionOwner";
    public const string ColumnAuthors = "BackOffice.Submissions.Management.Editor.Table.Authors";
    public const string ColumnPayment = "BackOffice.Submissions.Management.Editor.Table.Payment";
    public const string ColumnStatus = "BackOffice.Submissions.Management.Editor.Table.Status";
    public const string ColumnDate = "BackOffice.Submissions.Management.Editor.Table.Date";

    // ============================================================
    // List Actions
    // ============================================================

    public const string ManageButton = "BackOffice.Submissions.Management.Editor.Action.Manage";
    public const string EditButton = "BackOffice.Submissions.Management.EditButton";
    public const string DeleteButton = "BackOffice.Submissions.Management.DeleteButton";
    public const string DeleteConfirmTitle = "BackOffice.Submissions.Management.DeleteConfirmTitle";
    public const string DeleteConfirmText = "BackOffice.Submissions.Management.DeleteConfirmText";
    public const string DeleteConfirmButton = "BackOffice.Submissions.Management.DeleteConfirmButton";

    public const string DetailButton = "BackOffice.Submissions.Management.Editor.Action.Detail";
    public const string ReviewerAssignmentButton = "BackOffice.Submissions.Management.Editor.Action.ReviewerAssignment";
    public const string EvaluationsButton = "BackOffice.Submissions.Management.Editor.Action.Evaluations";
    public const string FilesButton = "BackOffice.Submissions.Management.Editor.Action.Files";
    public const string HistoryButton = "BackOffice.Submissions.Management.Editor.Action.History";
    public const string PaymentActionButton = "BackOffice.Submissions.Management.Editor.Action.Payment";

    // ============================================================
    // Common / Meta
    // ============================================================

    public const string Orcid = "BackOffice.Submissions.Management.Editor.Field.Orcid";
    public const string CorrespondingShort = "BackOffice.Submissions.Management.Editor.Author.CorrespondingShort";
    public const string AuthorCountFormat = "BackOffice.Submissions.Management.Editor.Author.CountFormat";
    public const string AdditionalAuthorCountFormat = "BackOffice.Submissions.Management.Editor.Author.AdditionalCountFormat";
    public const string OwnerSubmissionCountFormat = "BackOffice.Submissions.Management.Editor.Owner.SubmissionCountFormat";
    public const string OwnerMultipleBadgeTitle = "BackOffice.Submissions.Management.Editor.Owner.MultipleBadgeTitle";
    public const string OwnerMissing = "BackOffice.Submissions.Management.Editor.Owner.Missing";
    public const string BackToList = "BackOffice.Submissions.Management.Editor.Action.BackToList";
    public const string Print = "BackOffice.Submissions.Management.Editor.Action.Print";

    public const string Congress = "BackOffice.Submissions.Management.Editor.Meta.Congress";
    public const string CorrespondingAuthor = "BackOffice.Submissions.Management.Editor.Meta.CorrespondingAuthor";
    public const string SubmittedAt = "BackOffice.Submissions.Management.Editor.Meta.SubmittedAt";

    public const string Draft = "BackOffice.Submissions.Management.Editor.Common.Draft";

    // ============================================================
    // Focus Area
    // ============================================================

    public const string FocusTitle = "BackOffice.Submissions.Management.Editor.Focus.Title";
    public const string FocusDescription = "BackOffice.Submissions.Management.Editor.Focus.Description";
    public const string ReviewCompletionFormat = "BackOffice.Submissions.Management.Editor.Focus.ReviewCompletionFormat";

    public const string FocusPendingTitle = "BackOffice.Submissions.Management.Editor.Focus.PendingTitle";
    public const string FocusPendingDescription = "BackOffice.Submissions.Management.Editor.Focus.PendingDescription";
    public const string FocusPendingBadge = "BackOffice.Submissions.Management.Editor.Focus.PendingBadge";

    public const string FocusAcceptedTitle = "BackOffice.Submissions.Management.Editor.Focus.AcceptedTitle";
    public const string FocusAcceptedDescription = "BackOffice.Submissions.Management.Editor.Focus.AcceptedDescription";
    public const string FocusAcceptedBadge = "BackOffice.Submissions.Management.Editor.Focus.AcceptedBadge";

    public const string FocusRejectedTitle = "BackOffice.Submissions.Management.Editor.Focus.RejectedTitle";
    public const string FocusRejectedDescription = "BackOffice.Submissions.Management.Editor.Focus.RejectedDescription";
    public const string FocusRejectedBadge = "BackOffice.Submissions.Management.Editor.Focus.RejectedBadge";

    public const string FocusPaymentTitle = "BackOffice.Submissions.Management.Editor.Focus.PaymentTitle";
    public const string FocusPaymentDescription = "BackOffice.Submissions.Management.Editor.Focus.PaymentDescription";
    public const string FocusPaymentBadge = "BackOffice.Submissions.Management.Editor.Focus.PaymentBadge";

    public const string FocusCompletedTitle = "BackOffice.Submissions.Management.Editor.Focus.CompletedTitle";
    public const string FocusCompletedDescription = "BackOffice.Submissions.Management.Editor.Focus.CompletedDescription";
    public const string FocusCompletedBadge = "BackOffice.Submissions.Management.Editor.Focus.CompletedBadge";

    // ============================================================
    // Header Actions
    // ============================================================

    public const string ActionMakeDecision = "BackOffice.Submissions.Management.Editor.Action.MakeDecision";
    public const string ActionAssignReviewer = "BackOffice.Submissions.Management.Editor.Action.AssignReviewer";
    public const string ActionViewEvaluations = "BackOffice.Submissions.Management.Editor.Action.ViewEvaluations";
    public const string ActionReviewEvaluations = "BackOffice.Submissions.Management.Editor.Action.ReviewEvaluations";
    public const string ActionFinalDecision = "BackOffice.Submissions.Management.Editor.Action.FinalDecision";

    public const string ActionApprove = "BackOffice.Submissions.Management.Editor.Action.Approve";
    public const string ActionReject = "BackOffice.Submissions.Management.Editor.Action.Reject";
    public const string ActionEditorEvaluation = "BackOffice.Submissions.Management.Editor.Action.EditorEvaluation";
    public const string ActionMarkPaymentCompleted = "BackOffice.Submissions.Management.Action.MarkPaymentCompleted";
    public const string ActionRevertPayment = "BackOffice.Submissions.Management.Action.RevertPayment";
    public const string ActionRestartProcess = "BackOffice.Submissions.Management.Editor.Action.RestartProcess";

    // ============================================================
    // Stat Cards
    // ============================================================

    public const string StatEditorStatus = "BackOffice.Submissions.Management.Editor.Stat.EditorStatus";
    public const string StatReviewerStatus = "BackOffice.Submissions.Management.Editor.Stat.ReviewerStatus";
    public const string ReviewCompletedFormat = "BackOffice.Submissions.Management.Editor.Stat.ReviewCompletedFormat";
    public const string StatAverageScore = "BackOffice.Submissions.Management.Editor.Stat.AverageScore";
    public const string StatLastAction = "BackOffice.Submissions.Management.Editor.Stat.LastAction";

    // ============================================================
    // Tabs
    // ============================================================

    public const string TabSummary = "BackOffice.Submissions.Management.Editor.Tab.Summary";
    public const string TabSummaryDescription = "BackOffice.Submissions.Management.Editor.Tab.SummaryDescription";

    public const string TabReviewerAssignment = "BackOffice.Submissions.Management.Editor.Tab.ReviewerAssignment";
    public const string TabReviewerAssignmentDescription = "BackOffice.Submissions.Management.Editor.Tab.ReviewerAssignmentDescription";

    public const string TabEvaluations = "BackOffice.Submissions.Management.Editor.Tab.Evaluations";
    public const string TabEvaluationsDescription = "BackOffice.Submissions.Management.Editor.Tab.EvaluationsDescription";

    public const string TabDecision = "BackOffice.Submissions.Management.Editor.Tab.Decision";
    public const string TabDecisionDescription = "BackOffice.Submissions.Management.Editor.Tab.DecisionDescription";

    public const string TabPayment = "BackOffice.Submissions.Management.Tab.Payment";
    public const string TabPaymentDescription = "BackOffice.Submissions.Management.Tab.Payment.Description";

    public const string TabFiles = "BackOffice.Submissions.Management.Editor.Tab.Files";
    public const string TabFilesDescription = "BackOffice.Submissions.Management.Editor.Tab.FilesDescription";

    public const string TabHistory = "BackOffice.Submissions.Management.Editor.Tab.History";
    public const string TabHistoryDescription = "BackOffice.Submissions.Management.Editor.Tab.HistoryDescription";

    // ============================================================
    // Summary
    // ============================================================

    public const string SubmissionInfoTitle = "BackOffice.Submissions.Management.Editor.Summary.SubmissionInfoTitle";
    public const string SubmissionInfoDescription = "BackOffice.Submissions.Management.Editor.Summary.SubmissionInfoDescription";

    public const string SubmissionNumber = "BackOffice.Submissions.Management.Editor.Field.SubmissionNumber";
    public const string SubmissionType = "BackOffice.Submissions.Management.Editor.Field.SubmissionType";
    public const string Topic = "BackOffice.Submissions.Management.Editor.Field.Topic";
    public const string PaymentStatus = "BackOffice.Submissions.Management.Editor.Field.PaymentStatus";
    public const string Abstract = "BackOffice.Submissions.Management.Editor.Field.Abstract";

    public const string AuthorsTitle = "BackOffice.Submissions.Management.Editor.Summary.AuthorsTitle";
    public const string AuthorsDescription = "BackOffice.Submissions.Management.Editor.Summary.AuthorsDescription";
    public const string AuthorsEmpty = "BackOffice.Submissions.Management.Editor.Summary.AuthorsEmpty";

    public const string AuthorRoleCorresponding = "BackOffice.Submissions.Management.Editor.Author.Role.Corresponding";
    public const string AuthorRoleDefault = "BackOffice.Submissions.Management.Editor.Author.Role.Default";
    public const string AuthorBadgeCorresponding = "BackOffice.Submissions.Management.Editor.Author.Badge.Corresponding";
    public const string AuthorBadgeCoAuthor = "BackOffice.Submissions.Management.Editor.Author.Badge.CoAuthor";

    public const string SuggestedFlowTitle = "BackOffice.Submissions.Management.Editor.Summary.SuggestedFlowTitle";
    public const string SuggestedFlowDescription = "BackOffice.Submissions.Management.Editor.Summary.SuggestedFlowDescription";
    public const string QuickActions = "BackOffice.Submissions.Management.Editor.Summary.QuickActions";

    // ============================================================
    // Reviewer Assignment
    // ============================================================

    public const string AssignedReviewersTitle = "BackOffice.Submissions.Management.Editor.Reviewers.AssignedTitle";
    public const string AssignedReviewersDescription = "BackOffice.Submissions.Management.Editor.Reviewers.AssignedDescription";
    public const string ReviewerCountFormat = "BackOffice.Submissions.Management.Editor.Reviewers.CountFormat";
    public const string NoAssignedReviewers = "BackOffice.Submissions.Management.Editor.Reviewers.Empty";
    public const string AssignedAt = "BackOffice.Submissions.Management.Editor.Reviewers.AssignedAt";
    public const string ReviewCompleted = "BackOffice.Submissions.Management.Editor.Reviewers.Completed";
    public const string ReviewWaiting = "BackOffice.Submissions.Management.Editor.Reviewers.Waiting";

    public const string ReviewerAssignmentInfoTitle = "BackOffice.Submissions.Management.Editor.Reviewers.InfoTitle";
    public const string ReviewerAssignmentInfoDescription = "BackOffice.Submissions.Management.Editor.Reviewers.InfoDescription";

    public const string NewReviewerTitle = "BackOffice.Submissions.Management.Editor.Reviewers.NewTitle";
    public const string NewReviewerDescription = "BackOffice.Submissions.Management.Editor.Reviewers.NewDescription";
    public const string NoReviewerCandidate = "BackOffice.Submissions.Management.Editor.Reviewers.NoCandidate";
    public const string UserList = "BackOffice.Submissions.Management.Editor.Reviewers.UserList";
    public const string ReviewerLabel = "BackOffice.Submissions.Management.Editor.Reviewers.Label.Reviewer";
    public const string SelectReviewer = "BackOffice.Submissions.Management.Editor.Reviewers.SelectReviewer";
    public const string InstitutionEmpty = "BackOffice.Submissions.Management.Editor.Common.InstitutionEmpty";
    public const string WillBeAddedToPool = "BackOffice.Submissions.Management.Editor.Reviewers.WillBeAddedToPool";
    public const string DueDate = "BackOffice.Submissions.Management.Editor.Reviewers.DueDate";
    public const string DueDateHelp = "BackOffice.Submissions.Management.Editor.Reviewers.DueDateHelp";
    public const string ReviewerNote = "BackOffice.Submissions.Management.Editor.Reviewers.Note";
    public const string ReviewerNotePlaceholder = "BackOffice.Submissions.Management.Editor.Reviewers.NotePlaceholder";
    public const string ReviewerAssignInfo = "BackOffice.Submissions.Management.Editor.Reviewers.AssignInfo";
    public const string ReviewerRequiredMessage = "BackOffice.Submissions.Management.Editor.Reviewers.RequiredMessage";

    public const string ReviewerAssignmentDisabledAfterEvaluation = "BackOffice.Submissions.Management.ReviewerAssignment.DisabledAfterEvaluation";

    // ============================================================
    // Evaluations
    // ============================================================

    public const string EvaluationsInfoTitle = "BackOffice.Submissions.Management.Editor.Evaluations.InfoTitle";
    public const string EvaluationsInfoDescription = "BackOffice.Submissions.Management.Editor.Evaluations.InfoDescription";
    public const string EvaluationsEmpty = "BackOffice.Submissions.Management.Editor.Evaluations.Empty";
    public const string EvaluationDate = "BackOffice.Submissions.Management.Editor.Evaluations.Date";
    public const string RecommendationEmpty = "BackOffice.Submissions.Management.Editor.Evaluations.RecommendationEmpty";
    public const string Score = "BackOffice.Submissions.Management.Editor.Evaluations.Score";
    public const string Criterion = "BackOffice.Submissions.Management.Editor.Evaluations.Criterion";
    public const string CommentEmpty = "BackOffice.Submissions.Management.Editor.Evaluations.CommentEmpty";

    // ============================================================
    // Decision
    // ============================================================

    public const string DecisionTitle = "BackOffice.Submissions.Management.Editor.Decision.Title";
    public const string DecisionDescription = "BackOffice.Submissions.Management.Editor.Decision.Description";
    public const string DecisionNoAction = "BackOffice.Submissions.Management.Editor.Decision.NoAction";
    public const string DecisionConfirmText = "BackOffice.Submissions.Management.Editor.Decision.ConfirmText";
    public const string DecisionConfirmButton = "BackOffice.Submissions.Management.Editor.Decision.ConfirmButton";
    public const string DecisionActionBadge = "BackOffice.Submissions.Management.Editor.Decision.ActionBadge";

    public const string DecisionPublicNote = "BackOffice.Submissions.Management.Editor.Decision.PublicNote";
    public const string DecisionPublicNotePlaceholder = "BackOffice.Submissions.Management.Editor.Decision.PublicNotePlaceholder";
    public const string DecisionInternalNote = "BackOffice.Submissions.Management.Editor.Decision.InternalNote";
    public const string DecisionInternalNotePlaceholder = "BackOffice.Submissions.Management.Editor.Decision.InternalNotePlaceholder";
    public const string DecisionDisabledDefault = "BackOffice.Submissions.Management.Editor.Decision.DisabledDefault";

    public const string DecisionEvaluationRequired = "BackOffice.Submissions.Management.Editor.Decision.EvaluationRequired";

    public const string DecisionApproveModalTitle = "BackOffice.Submissions.Management.Editor.Decision.Approve.ModalTitle";
    public const string DecisionApproveModalText = "BackOffice.Submissions.Management.Editor.Decision.Approve.ModalText";
    public const string DecisionApproveSubmit = "BackOffice.Submissions.Management.Editor.Decision.Approve.Submit";

    public const string DecisionRejectModalTitle = "BackOffice.Submissions.Management.Editor.Decision.Reject.ModalTitle";
    public const string DecisionRejectModalText = "BackOffice.Submissions.Management.Editor.Decision.Reject.ModalText";
    public const string DecisionRejectSubmit = "BackOffice.Submissions.Management.Editor.Decision.Reject.Submit";

    public const string DecisionRestartModalTitle = "BackOffice.Submissions.Management.Editor.Decision.Restart.ModalTitle";
    public const string DecisionRestartModalText = "BackOffice.Submissions.Management.Editor.Decision.Restart.ModalText";
    public const string DecisionRestartSubmit = "BackOffice.Submissions.Management.Editor.Decision.Restart.Submit";

    public const string BeforeDecisionTitle = "BackOffice.Submissions.Management.Editor.Decision.BeforeTitle";
    public const string BeforeDecisionDescription = "BackOffice.Submissions.Management.Editor.Decision.BeforeDescription";
    public const string ReviewerSummaryTitle = "BackOffice.Submissions.Management.Editor.Decision.ReviewerSummaryTitle";
    public const string NoEvaluationYet = "BackOffice.Submissions.Management.Editor.Decision.NoEvaluationYet";
    public const string DecisionSuggestionLabel = "BackOffice.Submissions.Management.Editor.Decision.SuggestionLabel";
    public const string DecisionSuggestionText = "BackOffice.Submissions.Management.Editor.Decision.SuggestionText";

    public const string TransitionAccept = "BackOffice.Submissions.Management.Editor.Decision.Transition.Accept";
    public const string TransitionReject = "BackOffice.Submissions.Management.Editor.Decision.Transition.Reject";
    public const string TransitionRevision = "BackOffice.Submissions.Management.Editor.Decision.Transition.Revision";
    public const string TransitionSendToReview = "BackOffice.Submissions.Management.Editor.Decision.Transition.SendToReview";
    public const string TransitionPayment = "BackOffice.Submissions.Management.Editor.Decision.Transition.Payment";
    public const string TransitionComplete = "BackOffice.Submissions.Management.Editor.Decision.Transition.Complete";
    public const string TransitionWithdraw = "BackOffice.Submissions.Management.Editor.Decision.Transition.Withdraw";
    public const string TransitionDefault = "BackOffice.Submissions.Management.Editor.Decision.Transition.Default";

    // ============================================================
    // Payment
    // ============================================================

    public const string PaymentTitle = "BackOffice.Submissions.Management.Payment.Title";
    public const string PaymentDescription = "BackOffice.Submissions.Management.Payment.Description";

    public const string PaymentDocumentsTitle = "BackOffice.Submissions.Management.Payment.Documents.Title";
    public const string PaymentDocumentsEmpty = "BackOffice.Submissions.Management.Payment.Documents.Empty";
    public const string PaymentDocumentPending = "BackOffice.Submissions.Management.Payment.Document.Pending";
    public const string PaymentDocumentApproved = "BackOffice.Submissions.Management.Payment.Document.Approved";

    public const string PaymentManualConfirmTitle = "BackOffice.Submissions.Management.Payment.ManualConfirm.Title";
    public const string PaymentManualConfirmDescription = "BackOffice.Submissions.Management.Payment.ManualConfirm.Description";

    public const string PaymentRevertConfirmTitle = "BackOffice.Submissions.Management.Payment.RevertConfirm.Title";
    public const string PaymentRevertConfirmDescription = "BackOffice.Submissions.Management.Payment.RevertConfirm.Description";
    public const string PaymentRevertSuccessMessage = "BackOffice.Submissions.Management.Payment.Revert.Success";
    public const string PaymentRevertErrorMessage = "BackOffice.Submissions.Management.Payment.Revert.Error";

    // ============================================================
    // Files
    // ============================================================

    public const string FilesTitle = "BackOffice.Submissions.Management.Editor.Files.Title";
    public const string FilesDescription = "BackOffice.Submissions.Management.Editor.Files.Description";
    public const string FilesCountFormat = "BackOffice.Submissions.Management.Editor.Files.CountFormat";
    public const string FilesEmpty = "BackOffice.Submissions.Management.Editor.Files.Empty";
    public const string UploadedAt = "BackOffice.Submissions.Management.Editor.Files.UploadedAt";
    public const string Download = "BackOffice.Submissions.Management.Editor.Files.Download";

    // ============================================================
    // History
    // ============================================================

    public const string HistoryTitle = "BackOffice.Submissions.Management.Editor.History.Title";
    public const string HistoryDescription = "BackOffice.Submissions.Management.Editor.History.Description";
    public const string HistoryCountFormat = "BackOffice.Submissions.Management.Editor.History.CountFormat";
    public const string HistoryEmpty = "BackOffice.Submissions.Management.Editor.History.Empty";
    public const string HistoryGenericTitle = "BackOffice.Submissions.Management.Editor.History.GenericTitle";
    public const string HistoryDraftTitle = "BackOffice.Submissions.Management.Editor.History.DraftTitle";
    public const string HistorySubmittedTitle = "BackOffice.Submissions.Management.Editor.History.SubmittedTitle";
    public const string HistoryReviewAssignmentTitle = "BackOffice.Submissions.Management.Editor.History.ReviewAssignmentTitle";
    public const string HistoryReviewStartedTitle = "BackOffice.Submissions.Management.Editor.History.ReviewStartedTitle";
    public const string HistoryEditorialDecisionTitle = "BackOffice.Submissions.Management.Editor.History.EditorialDecisionTitle";
    public const string HistoryAcceptedTitle = "BackOffice.Submissions.Management.Editor.History.AcceptedTitle";
    public const string HistoryPaymentTitle = "BackOffice.Submissions.Management.Editor.History.PaymentTitle";
    public const string HistoryCompletedTitle = "BackOffice.Submissions.Management.Editor.History.CompletedTitle";
    public const string HistoryRejectedTitle = "BackOffice.Submissions.Management.Editor.History.RejectedTitle";
    public const string HistoryWithdrawnTitle = "BackOffice.Submissions.Management.Editor.History.WithdrawnTitle";
    public const string HistoryPaymentCompletedTitle = "BackOffice.Submissions.Management.Editor.History.PaymentCompletedTitle";
    public const string HistoryPaymentRevertedTitle = "BackOffice.Submissions.Management.Editor.History.PaymentRevertedTitle";
    public const string HistoryReviewerAssignedTitle = "BackOffice.Submissions.Management.Editor.History.ReviewerAssignedTitle";
    public const string HistoryReviewCompletedTitle = "BackOffice.Submissions.Management.Editor.History.ReviewCompletedTitle";
    public const string HistoryNoNote = "BackOffice.Submissions.Management.Editor.History.NoNote";
    public const string HistoryPerformedBy = "BackOffice.Submissions.Management.Editor.History.PerformedBy";
}
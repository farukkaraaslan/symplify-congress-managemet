namespace Symplify.BackOffice.WebUI.Models.ReviewerEvaluations;

public sealed class ReviewerEvaluationFilterOptionsViewModel
{
    public IReadOnlyList<ReviewerEvaluationFilterOptionViewModel> Congresses { get; set; } = Array.Empty<ReviewerEvaluationFilterOptionViewModel>();

    public IReadOnlyList<ReviewerEvaluationFilterOptionViewModel> Statuses { get; set; } = Array.Empty<ReviewerEvaluationFilterOptionViewModel>();

    public IReadOnlyList<ReviewerEvaluationFilterOptionViewModel> Topics { get; set; } = Array.Empty<ReviewerEvaluationFilterOptionViewModel>();

    public IReadOnlyList<ReviewerEvaluationFilterOptionViewModel> SubmissionTypes { get; set; } = Array.Empty<ReviewerEvaluationFilterOptionViewModel>();
}

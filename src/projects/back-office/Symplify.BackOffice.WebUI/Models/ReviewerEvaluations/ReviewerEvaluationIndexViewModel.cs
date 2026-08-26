using Symplify.BackOffice.Application.Features.ReviewerEvaluations.Queries.GetList;

namespace Symplify.BackOffice.WebUI.Models.ReviewerEvaluations;

public sealed class ReviewerEvaluationIndexViewModel
{
    public GetReviewerEvaluationListResponse Evaluations { get; set; } = new();

    public ReviewerEvaluationFilterOptionsViewModel FilterOptions { get; set; } = new();

    public string? SearchText { get; set; }

    public Guid? CongressId { get; set; }

    public string? Status { get; set; }

    public Guid? TopicId { get; set; }

    public Guid? SubmissionTypeId { get; set; }
}

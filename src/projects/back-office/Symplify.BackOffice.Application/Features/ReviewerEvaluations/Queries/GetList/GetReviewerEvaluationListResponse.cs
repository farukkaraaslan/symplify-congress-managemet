namespace Symplify.BackOffice.Application.Features.ReviewerEvaluations.Queries.GetList;

public sealed class GetReviewerEvaluationListResponse
{
    public List<GetReviewerEvaluationListItemDto> Items { get; set; } = new();

    public int Count { get; set; }

    public int TotalCount => Count > 0 || Items.Count == 0 ? Count : Items.Count;
    public int PendingCount => Items.Count(item => !item.IsCompleted);
    public int InProgressCount => 0;
    public int CompletedCount => Items.Count(item => item.IsCompleted);
    public int DueSoonCount => Items.Count(item => !item.IsCompleted && item.IsDueSoon);
}

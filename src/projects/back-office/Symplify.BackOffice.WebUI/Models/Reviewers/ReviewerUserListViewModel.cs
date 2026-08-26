namespace Symplify.BackOffice.WebUI.Models.Reviewers;

public sealed class ReviewerUserListViewModel
{
    public string? SearchText { get; set; }
    public IReadOnlyList<ReviewerUserListItemViewModel> Users { get; set; } = Array.Empty<ReviewerUserListItemViewModel>();
}

public sealed class ReviewerUserListItemViewModel
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Institution { get; set; }
    public string? Orcid { get; set; }
    public bool IsBlacklisted { get; set; }
    public bool IsReviewer { get; set; }
    public Guid? ReviewerId { get; set; }
    public string ReviewerStatus { get; set; } = string.Empty;
}

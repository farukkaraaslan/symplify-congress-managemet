namespace Symplify.BackOffice.Application.Features.Reviewers.Queries.GetList;
public class GetListReviewerListItemDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Symplify.BackOffice.Domain.Enums.ReviewerStatus Status { get; set; }
    public bool IsActive { get; set; }
}

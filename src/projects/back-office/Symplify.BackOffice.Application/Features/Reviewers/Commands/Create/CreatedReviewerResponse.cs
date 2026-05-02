namespace Symplify.BackOffice.Application.Features.Reviewers.Commands.Create;
public class CreatedReviewerResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Symplify.BackOffice.Domain.Enums.ReviewerStatus Status { get; set; }
    public bool IsActive { get; set; }
}

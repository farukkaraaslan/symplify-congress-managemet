using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.Reviewers.Queries.GetList;

public class GetListReviewerListItemDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Institution { get; set; }
    public string? Orcid { get; set; }
    public ReviewerStatus Status { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
}

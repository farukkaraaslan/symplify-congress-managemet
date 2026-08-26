namespace Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Queries.GetSelectionList;

public sealed class GetCongressSubmissionTypeSelectionListItemDto
{
    public Guid SubmissionTypeId { get; set; }
    public Guid? CongressSubmissionTypeId { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public bool IsSelected { get; set; }
    public bool IsFallback { get; set; }
}

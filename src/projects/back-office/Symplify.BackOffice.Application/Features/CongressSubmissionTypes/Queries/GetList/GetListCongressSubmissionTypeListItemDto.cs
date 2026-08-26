namespace Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Queries.GetList;

public class GetListCongressSubmissionTypeListItemDto
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public Guid SubmissionTypeId { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public bool SubmissionTypeIsActive { get; set; }
    public Guid DisplayLanguageId { get; set; }
    public bool IsFallback { get; set; }
}

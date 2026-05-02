namespace Symplify.BackOffice.Application.Features.CongressImportantDates.Queries.GetList;
public class GetListCongressImportantDateListItemDto
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public DateTime Date { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid DisplayLanguageId { get; set; }
    public bool IsFallback { get; set; }
}

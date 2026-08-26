namespace Symplify.BackOffice.Application.Features.CongressSections.Queries.GetList;
public class GetListCongressSectionListItemDto
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public string BindingKey { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public Guid DisplayLanguageId { get; set; }
    public bool IsFallback { get; set; }
}

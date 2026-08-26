namespace Symplify.BackOffice.Application.Features.Regions.Queries.GetList;
public class GetListRegionListItemDto
{
    public Guid Id { get; set; }
    public Guid? CountryId { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid DisplayLanguageId { get; set; }
    public bool IsFallback { get; set; }
}

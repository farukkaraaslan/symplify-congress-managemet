namespace Symplify.BackOffice.Application.Features.Cities.Queries.GetList;
public class GetListCityListItemDto
{
    public Guid Id { get; set; }
    public Guid StateId { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid DisplayLanguageId { get; set; }
    public bool IsFallback { get; set; }
}

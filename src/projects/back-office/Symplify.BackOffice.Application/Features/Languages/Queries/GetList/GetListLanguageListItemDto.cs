namespace Symplify.BackOffice.Application.Features.Languages.Queries.GetList;
public class GetListLanguageListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Culture { get; set; } = string.Empty;
    public string? TwoLetterIsoCode { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
}

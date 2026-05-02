namespace Symplify.BackOffice.Application.Features.Countries.Queries.GetById;
public class GetByIdCountryResponse
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string? PhoneCode { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid DisplayLanguageId { get; set; }
    public bool IsFallback { get; set; }
}

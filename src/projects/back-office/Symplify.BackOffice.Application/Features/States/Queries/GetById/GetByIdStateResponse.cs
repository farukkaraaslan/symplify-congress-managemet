namespace Symplify.BackOffice.Application.Features.States.Queries.GetById;
public class GetByIdStateResponse
{
    public Guid Id { get; set; }
    public Guid CountryId { get; set; }
    public string? Code { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid DisplayLanguageId { get; set; }
    public bool IsFallback { get; set; }
}

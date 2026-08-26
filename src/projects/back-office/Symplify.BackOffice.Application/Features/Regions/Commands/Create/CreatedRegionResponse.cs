namespace Symplify.BackOffice.Application.Features.Regions.Commands.Create;
public class CreatedRegionResponse
{
    public Guid Id { get; set; }
    public Guid? CountryId { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
}

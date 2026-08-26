namespace Symplify.BackOffice.Application.Features.Cities.Commands.Create;
public class CreatedCityResponse
{
    public Guid Id { get; set; }
    public Guid StateId { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
}

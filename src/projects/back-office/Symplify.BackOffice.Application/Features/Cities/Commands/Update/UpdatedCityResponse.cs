namespace Symplify.BackOffice.Application.Features.Cities.Commands.Update;
public class UpdatedCityResponse
{
    public Guid Id { get; set; }
    public Guid StateId { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
}

namespace Symplify.BackOffice.Application.Features.Cities.Commands.Delete;
public class DeletedCityResponse
{
    public Guid Id { get; set; }
    public Guid StateId { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
}

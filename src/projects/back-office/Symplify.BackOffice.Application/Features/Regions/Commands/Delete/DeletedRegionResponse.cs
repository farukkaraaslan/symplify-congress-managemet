namespace Symplify.BackOffice.Application.Features.Regions.Commands.Delete;
public class DeletedRegionResponse
{
    public Guid Id { get; set; }
    public Guid? CountryId { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
}

namespace Symplify.BackOffice.Application.Features.States.Commands.Update;
public class UpdatedStateResponse
{
    public Guid Id { get; set; }
    public Guid CountryId { get; set; }
    public string? Code { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
}

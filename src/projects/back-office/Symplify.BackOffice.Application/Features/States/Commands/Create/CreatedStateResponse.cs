namespace Symplify.BackOffice.Application.Features.States.Commands.Create;
public class CreatedStateResponse
{
    public Guid Id { get; set; }
    public Guid CountryId { get; set; }
    public string? Code { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
}

namespace Symplify.BackOffice.Application.Features.Countries.Commands.Delete;
public class DeletedCountryResponse
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string? PhoneCode { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
}

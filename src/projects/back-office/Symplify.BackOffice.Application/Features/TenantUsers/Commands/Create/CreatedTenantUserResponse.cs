namespace Symplify.BackOffice.Application.Features.TenantUsers.Commands.Create;
public class CreatedTenantUserResponse
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid? DefaultCongressId { get; set; }
    public bool IsActive { get; set; }
}

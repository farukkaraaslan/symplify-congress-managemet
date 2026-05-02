namespace Symplify.BackOffice.Application.Features.TenantUsers.Commands.Delete;
public class DeletedTenantUserResponse
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid? DefaultCongressId { get; set; }
    public bool IsActive { get; set; }
}

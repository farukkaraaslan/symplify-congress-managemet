namespace Symplify.BackOffice.Application.Features.OrganizationUsers.Commands.Delete;
public class DeletedOrganizationUserResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public Guid? DefaultCongressId { get; set; }
    public bool IsActive { get; set; }
}

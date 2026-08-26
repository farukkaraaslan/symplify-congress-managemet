namespace Symplify.BackOffice.Application.Features.OrganizationMailConfigurations.Commands.Save;

public sealed class SaveOrganizationMailConfigurationResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public bool Created { get; set; }
}

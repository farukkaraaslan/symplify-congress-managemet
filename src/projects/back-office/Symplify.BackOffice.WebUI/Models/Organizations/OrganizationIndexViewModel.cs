namespace Symplify.BackOffice.WebUI.Models.Organizations;

public sealed class OrganizationIndexViewModel
{
    public CreateOrganizationViewModel CreateModel { get; set; } = new();
    public UpdateOrganizationViewModel UpdateModel { get; set; } = new();
}

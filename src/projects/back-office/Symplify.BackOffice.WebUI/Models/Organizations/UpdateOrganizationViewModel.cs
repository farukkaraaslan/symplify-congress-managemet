namespace Symplify.BackOffice.WebUI.Models.Organizations;

public sealed class UpdateOrganizationViewModel : CreateOrganizationViewModel
{
    public Guid Id { get; set; }

    public string? ExistingLogoLightPath { get; set; }

    public string? ExistingLogoDarkPath { get; set; }

    public string? ExistingLogoLightUrl { get; set; }

    public string? ExistingLogoDarkUrl { get; set; }

    // Backward-compatible alias for old modal/views. New screens use ExistingLogoLightPath.
    public string? ExistingLogoPath
    {
        get => ExistingLogoLightPath;
        set => ExistingLogoLightPath = value;
    }
}

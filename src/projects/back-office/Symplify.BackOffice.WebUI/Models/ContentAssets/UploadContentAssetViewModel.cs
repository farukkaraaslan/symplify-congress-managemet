using Microsoft.AspNetCore.Http;

namespace Symplify.BackOffice.WebUI.Models.ContentAssets;

public sealed class UploadContentAssetViewModel
{
    public Guid? CongressId { get; set; }
    public IFormFile? File { get; set; }
}

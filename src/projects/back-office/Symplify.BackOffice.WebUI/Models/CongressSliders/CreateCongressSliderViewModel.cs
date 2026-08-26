using Microsoft.AspNetCore.Http;

namespace Symplify.BackOffice.WebUI.Models.CongressSliders;

public sealed class CreateCongressSliderViewModel
{
    public Guid CongressId { get; set; }
    public int Order { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public IFormFile? ImageFile { get; set; }
    public List<CongressSliderTranslationViewModel> Translations { get; set; } = new();
}

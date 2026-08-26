using Microsoft.AspNetCore.Http;

namespace Symplify.BackOffice.WebUI.Models.CongressSliders;

public sealed class UpdateCongressSliderViewModel
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public string? ImagePreviewUrl { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
    public IFormFile? ImageFile { get; set; }
    public List<CongressSliderTranslationViewModel> Translations { get; set; } = new();
}

namespace Symplify.BackOffice.WebUI.Models.CongressSections;

public sealed class CreateCongressSectionViewModel
{
    public Guid CongressId { get; set; }

    public string BindingKey { get; set; } = string.Empty;

    public int Order { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public List<CongressSectionTranslationViewModel> Translations { get; set; } = new();
}

namespace Symplify.BackOffice.WebUI.Models.CongressSections;

public sealed class UpdateCongressSectionViewModel
{
    public Guid Id { get; set; }

    public Guid CongressId { get; set; }

    public string BindingKey { get; set; } = string.Empty;

    public int Order { get; set; }

    public bool IsActive { get; set; } = true;

    public List<CongressSectionTranslationViewModel> Translations { get; set; } = new();
}

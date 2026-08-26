namespace Symplify.BackOffice.WebUI.Models.Titles;

public sealed class CreateTitleViewModel
{
    public string? Code { get; set; }

    public bool IsActive { get; set; } = true;

    public List<CreateTitleTranslationViewModel> Translations { get; set; } = new();
}

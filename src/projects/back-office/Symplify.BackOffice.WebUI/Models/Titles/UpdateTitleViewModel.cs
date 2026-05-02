namespace Symplify.BackOffice.WebUI.Models.Titles;

public sealed class UpdateTitleViewModel
{
    public Guid Id { get; set; }

    public string? Code { get; set; }

    public bool IsActive { get; set; } = true;

    public List<UpdateTitleTranslationViewModel> Translations { get; set; } = new();
}

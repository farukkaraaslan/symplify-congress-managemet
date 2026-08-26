namespace Symplify.BackOffice.WebUI.Models.Titles;

public sealed class CreateTitleTranslationViewModel
{
    public Guid LanguageId { get; set; }

    public string Culture { get; set; } = string.Empty;

    public string LanguageName { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }
}

namespace Symplify.BackOffice.WebUI.Models.DocumentTypes;

public sealed class CreateDocumentTypeViewModel
{
    public string? Code { get; set; }

    public bool IsActive { get; set; } = true;

    public List<CreateDocumentTypeTranslationViewModel> Translations { get; set; } = new();
}

namespace Symplify.BackOffice.WebUI.Models.DocumentTypes;

public sealed class UpdateDocumentTypeViewModel
{
    public Guid Id { get; set; }

    public string? Code { get; set; }

    public bool IsActive { get; set; } = true;

    public List<UpdateDocumentTypeTranslationViewModel> Translations { get; set; } = new();
}

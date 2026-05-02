namespace Symplify.BackOffice.WebUI.Models.DocumentTypes;

public sealed class DocumentTypesIndexViewModel
{
    public CreateDocumentTypeViewModel CreateDocumentType { get; set; } = new();

    public UpdateDocumentTypeViewModel UpdateDocumentType { get; set; } = new();
}

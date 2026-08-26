using Microsoft.AspNetCore.Http;

namespace Symplify.BackOffice.WebUI.Models.CongressDocuments;

public sealed class CreateCongressDocumentViewModel
{
    public Guid CongressId { get; set; }

    public Guid? DocumentTypeId { get; set; }

    public IFormFile? File { get; set; }

    public IFormFile? CoverImage { get; set; }

    public bool IsActive { get; set; } = true;

    public List<DocumentTypeSelectItemViewModel> DocumentTypes { get; set; } = new();

    public List<CongressDocumentTranslationViewModel> Translations { get; set; } = new();
}

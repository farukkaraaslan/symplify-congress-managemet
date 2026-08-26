namespace Symplify.BackOffice.Application.Features.CongressDocuments.Queries.GetForUpdate;

public sealed class CongressDocumentTranslationForUpdateDto
{
    public Guid Id { get; set; }

    public Guid LanguageId { get; set; }

    public string? Description { get; set; }
}

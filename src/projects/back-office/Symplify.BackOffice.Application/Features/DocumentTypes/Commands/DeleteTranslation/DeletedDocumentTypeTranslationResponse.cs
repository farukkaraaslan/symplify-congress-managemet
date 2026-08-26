namespace Symplify.BackOffice.Application.Features.DocumentTypes.Commands.DeleteTranslation;
public class DeletedDocumentTypeTranslationResponse
{
    public Guid Id { get; set; }
    public Guid DocumentTypeId { get; set; }
    public Guid LanguageId { get; set; }
}

namespace Symplify.BackOffice.Application.Features.CongressSections.Commands.DeleteTranslation;
public class DeletedCongressSectionTranslationResponse
{
    public Guid Id { get; set; }
    public Guid CongressSectionId { get; set; }
    public Guid LanguageId { get; set; }
}

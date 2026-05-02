namespace Symplify.BackOffice.Application.Features.CongressImportantDates.Commands.DeleteTranslation;
public class DeletedCongressImportantDateTranslationResponse
{
    public Guid Id { get; set; }
    public Guid CongressImportantDateId { get; set; }
    public Guid LanguageId { get; set; }
}

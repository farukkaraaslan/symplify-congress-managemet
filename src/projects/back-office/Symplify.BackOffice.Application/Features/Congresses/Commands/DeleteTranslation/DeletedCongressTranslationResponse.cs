namespace Symplify.BackOffice.Application.Features.Congresses.Commands.DeleteTranslation;
public class DeletedCongressTranslationResponse
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public Guid LanguageId { get; set; }
}

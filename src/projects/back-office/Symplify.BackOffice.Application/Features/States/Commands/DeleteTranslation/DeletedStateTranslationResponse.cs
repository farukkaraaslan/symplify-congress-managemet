namespace Symplify.BackOffice.Application.Features.States.Commands.DeleteTranslation;
public class DeletedStateTranslationResponse
{
    public Guid Id { get; set; }
    public Guid StateId { get; set; }
    public Guid LanguageId { get; set; }
}

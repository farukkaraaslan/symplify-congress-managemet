namespace Symplify.BackOffice.Application.Features.CongressBoards.Commands.DeleteTranslation;
public class DeletedCongressBoardTranslationResponse
{
    public Guid Id { get; set; }
    public Guid CongressBoardId { get; set; }
    public Guid LanguageId { get; set; }
}

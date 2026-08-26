namespace Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands.DeleteTranslation;
public class DeletedCongressBoardMemberTranslationResponse
{
    public Guid Id { get; set; }
    public Guid CongressBoardMemberId { get; set; }
    public Guid LanguageId { get; set; }
}

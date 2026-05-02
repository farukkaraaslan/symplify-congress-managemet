namespace Symplify.BackOffice.Application.Features.EventRooms.Commands.DeleteTranslation;
public class DeletedEventRoomTranslationResponse
{
    public Guid Id { get; set; }
    public Guid EventRoomId { get; set; }
    public Guid LanguageId { get; set; }
}

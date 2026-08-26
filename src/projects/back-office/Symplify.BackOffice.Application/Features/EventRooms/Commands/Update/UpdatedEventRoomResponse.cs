namespace Symplify.BackOffice.Application.Features.EventRooms.Commands.Update;
public class UpdatedEventRoomResponse
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}

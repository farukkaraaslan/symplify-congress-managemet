namespace Symplify.BackOffice.WebUI.Models.EventRooms;

public sealed class EventRoomsIndexViewModel
{
    public CreateEventRoomViewModel CreateEventRoom { get; set; } = new();

    public UpdateEventRoomViewModel UpdateEventRoom { get; set; } = new();
}

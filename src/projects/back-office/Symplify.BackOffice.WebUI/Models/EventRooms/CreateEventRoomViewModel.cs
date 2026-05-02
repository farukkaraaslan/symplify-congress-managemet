namespace Symplify.BackOffice.WebUI.Models.EventRooms;

public sealed class CreateEventRoomViewModel
{
    public string? Code { get; set; }

    public bool IsActive { get; set; } = true;

    public List<CreateEventRoomTranslationViewModel> Translations { get; set; } = new();
}

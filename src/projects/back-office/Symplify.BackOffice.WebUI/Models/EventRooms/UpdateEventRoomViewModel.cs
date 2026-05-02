namespace Symplify.BackOffice.WebUI.Models.EventRooms;

public sealed class UpdateEventRoomViewModel
{
    public Guid Id { get; set; }

    public string? Code { get; set; }

    public bool IsActive { get; set; } = true;

    public List<UpdateEventRoomTranslationViewModel> Translations { get; set; } = new();
}

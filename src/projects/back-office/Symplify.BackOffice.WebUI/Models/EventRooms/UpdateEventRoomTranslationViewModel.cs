namespace Symplify.BackOffice.WebUI.Models.EventRooms;

public sealed class UpdateEventRoomTranslationViewModel
{
    public Guid LanguageId { get; set; }

    public string Culture { get; set; } = string.Empty;

    public string LanguageName { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public bool Exists { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }
}

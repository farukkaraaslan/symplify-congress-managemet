namespace Symplify.BackOffice.WebUI.Models.CongressBoards;

public sealed class CreateCongressBoardManageViewModel
{
    public Guid CongressId { get; set; }

    public bool IsActive { get; set; } = true;

    public List<CongressBoardTranslationViewModel> Translations { get; set; } = new();
}

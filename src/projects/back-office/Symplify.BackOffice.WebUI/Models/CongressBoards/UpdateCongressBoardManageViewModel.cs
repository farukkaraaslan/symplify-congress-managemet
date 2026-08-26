namespace Symplify.BackOffice.WebUI.Models.CongressBoards;

public sealed class UpdateCongressBoardManageViewModel
{
    public Guid Id { get; set; }

    public Guid CongressId { get; set; }

    public int Order { get; set; }

    public bool IsActive { get; set; } = true;

    public List<CongressBoardTranslationViewModel> Translations { get; set; } = new();
}

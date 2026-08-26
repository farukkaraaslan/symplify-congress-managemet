namespace Symplify.BackOffice.WebUI.Models.Titles;

public sealed class TitlesIndexViewModel
{
    public CreateTitleViewModel CreateTitle { get; set; } = new();

    public UpdateTitleViewModel UpdateTitle { get; set; } = new();
}

namespace Symplify.BackOffice.WebUI.Models.CongressImportantDates;

public sealed class CreateCongressImportantDateViewModel
{
    public Guid CongressId { get; set; }

    public string? StartDateText { get; set; }

    public string? EndDateText { get; set; }

    public int Order { get; set; }

    public bool IsActive { get; set; } = true;

    public List<CongressImportantDateTranslationViewModel> Translations { get; set; } = new();
}

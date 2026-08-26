namespace Symplify.BackOffice.WebUI.Models.Topics;

public sealed class CreateTopicViewModel
{
    public string? Code { get; set; }

    public bool IsActive { get; set; } = true;

    public List<CreateTopicTranslationViewModel> Translations { get; set; } = new();
}

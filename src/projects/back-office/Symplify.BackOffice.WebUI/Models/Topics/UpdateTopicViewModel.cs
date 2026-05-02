namespace Symplify.BackOffice.WebUI.Models.Topics;

public sealed class UpdateTopicViewModel
{
    public Guid Id { get; set; }

    public string? Code { get; set; }

    public bool IsActive { get; set; } = true;

    public List<UpdateTopicTranslationViewModel> Translations { get; set; } = new();
}

namespace Symplify.BackOffice.WebUI.Models.CongressTopics;

public sealed class SaveCongressTopicSelectionsViewModel
{
    public Guid CongressId { get; set; }
    public List<Guid> SelectedTopicIds { get; set; } = new();
    public List<Guid?> SelectedCategoryIds { get; set; } = new();
}

namespace Symplify.BackOffice.WebUI.Models.Topics;

public sealed class TopicsIndexViewModel
{
    public CreateTopicViewModel CreateTopic { get; set; } = new();

    public UpdateTopicViewModel UpdateTopic { get; set; } = new();
}

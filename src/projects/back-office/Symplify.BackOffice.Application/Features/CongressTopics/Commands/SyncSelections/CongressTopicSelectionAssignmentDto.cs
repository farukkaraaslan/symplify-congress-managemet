namespace Symplify.BackOffice.Application.Features.CongressTopics.Commands.SyncSelections;

public sealed class CongressTopicSelectionAssignmentDto
{
    public Guid TopicId { get; set; }
    public Guid? CategoryId { get; set; }
}

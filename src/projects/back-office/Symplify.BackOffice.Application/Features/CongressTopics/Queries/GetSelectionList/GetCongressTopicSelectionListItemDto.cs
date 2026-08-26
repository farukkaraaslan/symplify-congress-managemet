namespace Symplify.BackOffice.Application.Features.CongressTopics.Queries.GetSelectionList;

public sealed class GetCongressTopicSelectionListItemDto
{
    public Guid TopicId { get; set; }
    public Guid? CongressTopicId { get; set; }
    public Guid? CategoryId { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public bool IsSelected { get; set; }
    public bool IsFallback { get; set; }
}

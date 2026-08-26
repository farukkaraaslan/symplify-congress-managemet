namespace Symplify.BackOffice.Application.Features.CongressTopics.Queries.GetList;

public class GetListCongressTopicListItemDto
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public Guid TopicId { get; set; }
    public Guid? CategoryId { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public bool TopicIsActive { get; set; }
    public Guid DisplayLanguageId { get; set; }
    public bool IsFallback { get; set; }
}

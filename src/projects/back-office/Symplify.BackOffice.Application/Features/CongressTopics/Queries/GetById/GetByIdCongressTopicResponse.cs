namespace Symplify.BackOffice.Application.Features.CongressTopics.Queries.GetById;
public class GetByIdCongressTopicResponse
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public Guid TopicId { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}

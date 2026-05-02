namespace Symplify.BackOffice.Application.Features.CongressTopics.Commands.Update;
public class UpdatedCongressTopicResponse
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public Guid TopicId { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}

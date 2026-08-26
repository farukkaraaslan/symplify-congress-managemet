namespace Symplify.BackOffice.Application.Features.Topics.Commands.Delete;
public class DeletedTopicResponse
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}

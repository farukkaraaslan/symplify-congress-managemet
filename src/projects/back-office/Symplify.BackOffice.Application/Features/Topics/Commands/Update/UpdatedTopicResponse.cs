namespace Symplify.BackOffice.Application.Features.Topics.Commands.Update;
public class UpdatedTopicResponse
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}

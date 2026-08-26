namespace Symplify.BackOffice.Application.Features.Topics.Commands.Create;
public class CreatedTopicResponse
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}

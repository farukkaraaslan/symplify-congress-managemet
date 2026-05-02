namespace Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands.Create;
public class CreatedCongressBoardMemberResponse
{
    public Guid Id { get; set; }
    public Guid CongressBoardId { get; set; }
    public string? ImagePath { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}

namespace Symplify.BackOffice.Application.Features.CongressBoards.Commands.Create;
public class CreatedCongressBoardResponse
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}

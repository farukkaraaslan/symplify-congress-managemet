namespace Symplify.BackOffice.Application.Features.CongressBoards.Commands.Update;
public class UpdatedCongressBoardResponse
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}

namespace Symplify.BackOffice.Application.Features.CongressBoards.Commands.Delete;
public class DeletedCongressBoardResponse
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}

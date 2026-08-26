namespace Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Commands.Delete;
public class DeletedCongressSubmissionTypeResponse
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public Guid SubmissionTypeId { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}

namespace Symplify.BackOffice.Application.Features.SubmissionTypes.Commands.Delete;
public class DeletedSubmissionTypeResponse
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}

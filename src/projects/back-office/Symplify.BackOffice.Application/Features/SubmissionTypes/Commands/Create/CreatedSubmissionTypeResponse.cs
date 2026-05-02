namespace Symplify.BackOffice.Application.Features.SubmissionTypes.Commands.Create;
public class CreatedSubmissionTypeResponse
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}

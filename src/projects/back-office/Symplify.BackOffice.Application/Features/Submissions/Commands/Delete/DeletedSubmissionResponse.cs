namespace Symplify.BackOffice.Application.Features.Submissions.Commands.Delete;

public sealed class DeletedSubmissionResponse
{
    public Guid Id { get; set; }

    public Guid CongressId { get; set; }

    public string SubmissionNumber { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
}

namespace Symplify.BackOffice.Application.Features.ExhibitionApplications.Commands.Create;

public sealed class CreatedExhibitionApplicationResponse
{
    public Guid Id { get; set; }

    public string SubmissionNumber { get; set; } = string.Empty;

    public Guid? SubmissionTypeId { get; set; }

    public bool IsSubmitted { get; set; }

    public DateTime? SubmittedAt { get; set; }
}

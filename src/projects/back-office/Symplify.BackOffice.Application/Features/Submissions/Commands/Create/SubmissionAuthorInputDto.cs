namespace Symplify.BackOffice.Application.Features.Submissions.Commands.Create;

public sealed class SubmissionAuthorInputDto
{
    public Guid? Id { get; set; }

    public Guid? TitleId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Institution { get; set; }

    public string? Orcid { get; set; }

    public bool IsCorrespondingAuthor { get; set; }
}

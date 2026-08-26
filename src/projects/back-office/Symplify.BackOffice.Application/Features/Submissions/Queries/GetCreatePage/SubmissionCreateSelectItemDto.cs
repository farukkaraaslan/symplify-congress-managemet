using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.Submissions.Queries.GetCreatePage;

public sealed class SubmissionCreateSelectItemDto
{
    public Guid Id { get; set; }

    public string Text { get; set; } = string.Empty;

    public SubmissionFormProfile FormProfile { get; set; } = SubmissionFormProfile.AcademicAbstract;
}

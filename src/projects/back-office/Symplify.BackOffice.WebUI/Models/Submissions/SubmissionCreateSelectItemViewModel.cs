using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.WebUI.Models.Submissions;

public sealed class SubmissionCreateSelectItemViewModel
{
    public Guid Id { get; set; }

    public string Text { get; set; } = string.Empty;

    public SubmissionFormProfile FormProfile { get; set; } = SubmissionFormProfile.AcademicAbstract;
}

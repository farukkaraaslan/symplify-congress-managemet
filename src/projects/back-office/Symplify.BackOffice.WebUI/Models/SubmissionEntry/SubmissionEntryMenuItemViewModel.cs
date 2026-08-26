using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.WebUI.Models.SubmissionEntry;

public sealed class SubmissionEntryMenuItemViewModel
{
    public Guid SubmissionTypeId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public SubmissionFormProfile FormProfile { get; set; } = SubmissionFormProfile.AcademicAbstract;

    public string Url { get; set; } = string.Empty;

    public string Icon { get; set; } = "solar:document-add-outline";

    public bool IsActive { get; set; }
}

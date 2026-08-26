namespace Symplify.BackOffice.WebUI.Models.SubmissionFinalFiles;

public sealed class ToggleProgramBookFileRequest
{
    public Guid FileId { get; set; }

    public bool IsIncludedInProgramBook { get; set; }
}

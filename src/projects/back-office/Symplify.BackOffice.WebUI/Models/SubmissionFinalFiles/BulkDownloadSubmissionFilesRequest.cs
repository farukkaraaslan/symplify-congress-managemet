namespace Symplify.BackOffice.WebUI.Models.SubmissionFinalFiles;

public sealed class BulkDownloadSubmissionFilesRequest
{
    public List<Guid> FileIds { get; set; } = new();
}

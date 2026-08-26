namespace Symplify.BackOffice.WebUI.Models.SubmissionFinalFiles;

public sealed class BulkDeleteSubmissionFinalFilesRequest
{
    public List<Guid> FileIds { get; set; } = new();
}

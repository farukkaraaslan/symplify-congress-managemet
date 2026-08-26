namespace Symplify.BackOffice.WebUI.Models.SubmissionFinalFiles;

public sealed class GenerateSubmissionFileShortLinksRequest
{
    public List<Guid> FileIds { get; set; } = new();
}

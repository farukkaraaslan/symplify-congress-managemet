using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.WebUI.Models.SubmissionFinalFiles;

public sealed class BulkReviewSubmissionFilesRequest
{
    public List<Guid> FileIds { get; set; } = new();

    public SubmissionFileReviewStatus ReviewStatus { get; set; }

    public string? ReviewNote { get; set; }
}

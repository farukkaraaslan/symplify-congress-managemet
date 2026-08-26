using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.WebUI.Models.SubmissionFinalFiles;

public sealed class ReviewSubmissionFileRequest
{
    public Guid FileId { get; set; }

    public SubmissionFileReviewStatus ReviewStatus { get; set; }

    public string? ReviewNote { get; set; }
}

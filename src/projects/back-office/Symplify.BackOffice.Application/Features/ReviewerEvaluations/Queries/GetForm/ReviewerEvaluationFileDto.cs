namespace Symplify.BackOffice.Application.Features.ReviewerEvaluations.Queries.GetForm;

public sealed class ReviewerEvaluationFileDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
    public string? FileType { get; set; }
    public long? FileSize { get; set; }
    public DateTime CreatedDate { get; set; }
}

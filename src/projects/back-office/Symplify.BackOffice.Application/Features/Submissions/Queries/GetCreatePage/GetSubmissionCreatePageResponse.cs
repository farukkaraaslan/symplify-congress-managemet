namespace Symplify.BackOffice.Application.Features.Submissions.Queries.GetCreatePage;

public sealed class GetSubmissionCreatePageResponse
{
    public Guid? SelectedCongressId { get; set; }

    public Guid? DefaultLanguageId { get; set; }

    public IReadOnlyList<SubmissionCreateSelectItemDto> Congresses { get; set; } = Array.Empty<SubmissionCreateSelectItemDto>();

    public IReadOnlyList<SubmissionCreateSelectItemDto> SubmissionTypes { get; set; } = Array.Empty<SubmissionCreateSelectItemDto>();

    public IReadOnlyList<SubmissionCreateSelectItemDto> Topics { get; set; } = Array.Empty<SubmissionCreateSelectItemDto>();

    public IReadOnlyList<SubmissionCreateSelectItemDto> Languages { get; set; } = Array.Empty<SubmissionCreateSelectItemDto>();

    public IReadOnlyList<SubmissionCreateSelectItemDto> Titles { get; set; } = Array.Empty<SubmissionCreateSelectItemDto>();
}

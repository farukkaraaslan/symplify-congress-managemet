namespace Symplify.BackOffice.Application.Features.Submissions.Queries.GetManagementFilterOptions;

public sealed class GetSubmissionManagementFilterOptionsResponse
{
    public IReadOnlyList<SubmissionManagementFilterOptionDto> Congresses { get; set; } = Array.Empty<SubmissionManagementFilterOptionDto>();

    public IReadOnlyList<SubmissionManagementFilterOptionDto> TransactionStatuses { get; set; } = Array.Empty<SubmissionManagementFilterOptionDto>();

    public IReadOnlyList<SubmissionManagementFilterOptionDto> PaymentStatuses { get; set; } = Array.Empty<SubmissionManagementFilterOptionDto>();

    public IReadOnlyList<SubmissionManagementFilterOptionDto> Topics { get; set; } = Array.Empty<SubmissionManagementFilterOptionDto>();

    public IReadOnlyList<SubmissionManagementFilterOptionDto> SubmissionTypes { get; set; } = Array.Empty<SubmissionManagementFilterOptionDto>();
}

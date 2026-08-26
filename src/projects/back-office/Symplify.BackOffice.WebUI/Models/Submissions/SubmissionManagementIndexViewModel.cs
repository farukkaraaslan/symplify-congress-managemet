using Core.Application.Responses;
using Symplify.BackOffice.Application.Features.Submissions.Queries.GetList;
using Symplify.BackOffice.Application.Features.Submissions.Queries.GetManagementFilterOptions;

namespace Symplify.BackOffice.WebUI.Models.Submissions;

public sealed class SubmissionManagementIndexViewModel
{
    public GetListResponse<GetListSubmissionListItemDto> Submissions { get; set; } = new();

    public GetSubmissionManagementFilterOptionsResponse FilterOptions { get; set; } = new();

    public string? SearchText { get; set; }

    public Guid? CongressId { get; set; }

    public Guid? SubmissionTypeId { get; set; }

    public Guid? TopicId { get; set; }

    public int? TransactionStatusId { get; set; }

    public int? PaymentStatusId { get; set; }

    public SubmissionOwnerMultiplicityFilter OwnerMultiplicity { get; set; } = SubmissionOwnerMultiplicityFilter.All;

    public bool ArchiveMode { get; set; }
}

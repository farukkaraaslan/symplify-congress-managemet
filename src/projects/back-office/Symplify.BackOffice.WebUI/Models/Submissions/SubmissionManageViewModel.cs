using Symplify.BackOffice.Application.Features.Submissions.Queries.GetManage;

namespace Symplify.BackOffice.WebUI.Models.Submissions;

public sealed class SubmissionManageViewModel
{
    public GetManageSubmissionResponse Detail { get; set; } = new();
}

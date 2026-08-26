namespace Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Commands.SyncSelections;

public sealed class SyncedCongressSubmissionTypeSelectionsResponse
{
    public int AddedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int DeletedCount { get; set; }
    public int SelectedCount { get; set; }
}

namespace Symplify.BackOffice.Application.Features.CongressTopics.Commands.SyncSelections;

public sealed class SyncedCongressTopicSelectionsResponse
{
    public int AddedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int DeletedCount { get; set; }
    public int SelectedCount { get; set; }
}

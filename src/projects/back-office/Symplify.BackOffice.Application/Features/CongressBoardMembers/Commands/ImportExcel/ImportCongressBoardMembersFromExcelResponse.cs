namespace Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands.ImportExcel;

public sealed class ImportCongressBoardMembersFromExcelResponse
{
    public int ImportedCount { get; set; }

    public int SkippedCount { get; set; }

    public List<string> Errors { get; set; } = new();
}

namespace Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands.ImportExcel;

public sealed class CongressBoardMemberExcelImportRowDto
{
    public int RowNumber { get; set; }

    public string? BoardName { get; set; }

    public string? AcademicTitle { get; set; }

    public string? FullName { get; set; }

    public string? Institution { get; set; }

    public int? Order { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Description { get; set; }
}

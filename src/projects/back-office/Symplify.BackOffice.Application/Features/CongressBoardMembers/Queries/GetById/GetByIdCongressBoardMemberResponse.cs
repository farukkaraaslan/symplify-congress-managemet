namespace Symplify.BackOffice.Application.Features.CongressBoardMembers.Queries.GetById;

public class GetByIdCongressBoardMemberResponse
{
    public Guid Id { get; set; }
    public Guid CongressBoardId { get; set; }
    public string? ImagePath { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }

    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// New root-field name used by the committee member UI.
    /// </summary>
    public string? AcademicTitle { get; set; }

    /// <summary>
    /// Backward-compatible alias used by the existing GetById query.
    /// </summary>
    public string? Title
    {
        get => AcademicTitle;
        set => AcademicTitle = value;
    }

    public string? Institution { get; set; }

    /// <summary>
    /// Existing localized description field kept for backward compatibility.
    /// In the new UI this corresponds to Role / Description.
    /// </summary>
    public string? Biography { get; set; }

    public Guid DisplayLanguageId { get; set; }
    public bool IsFallback { get; set; }
}

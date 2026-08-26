namespace Symplify.BackOffice.Application.Features.CongressBoardMembers.Queries.GetList;

public class GetListCongressBoardMemberListItemDto
{
    public Guid Id { get; set; }

    public Guid CongressId { get; set; }

    public Guid CongressBoardId { get; set; }

    public string BoardName { get; set; } = string.Empty;

    public string? ImagePath { get; set; }

    public bool HasImage { get; set; }

    public int Order { get; set; }

    public bool IsActive { get; set; }

    public bool IsAcceptanceLetterSigner { get; set; }

    public bool HasSignature { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? AcademicTitle { get; set; }

    public string? Institution { get; set; }

    public string? Description { get; set; }

    public Guid DisplayLanguageId { get; set; }

    public bool IsFallback { get; set; }
}

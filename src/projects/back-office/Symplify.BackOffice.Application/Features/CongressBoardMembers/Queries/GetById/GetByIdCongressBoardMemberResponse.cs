namespace Symplify.BackOffice.Application.Features.CongressBoardMembers.Queries.GetById;
public class GetByIdCongressBoardMemberResponse
{
    public Guid Id { get; set; }
    public Guid CongressBoardId { get; set; }
    public string? ImagePath { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Institution { get; set; }
    public string? Biography { get; set; }
    public Guid DisplayLanguageId { get; set; }
    public bool IsFallback { get; set; }
}

namespace Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands.Create;

public class CreatedCongressBoardMemberResponse
{
    public Guid Id { get; set; }
    public Guid CongressBoardId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AcademicTitle { get; set; }
    public string? Institution { get; set; }
    public string? ImagePath { get; set; }
    public string? ImageBucketName { get; set; }
    public string? ImageObjectName { get; set; }
    public string? ImageFileName { get; set; }
    public string? ImageContentType { get; set; }
    public long? ImageFileSize { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}

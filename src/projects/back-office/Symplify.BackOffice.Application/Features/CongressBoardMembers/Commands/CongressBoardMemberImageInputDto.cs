namespace Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands;

public sealed class CongressBoardMemberImageInputDto
{
    public string OriginalFileName { get; set; } = string.Empty;

    public string? ContentType { get; set; }

    public long Length { get; set; }

    public Stream Content { get; set; } = Stream.Null;
}

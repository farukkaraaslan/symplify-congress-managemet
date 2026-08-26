namespace Symplify.BackOffice.Application.Features.Organizations.Commands;

public sealed class OrganizationLogoInputDto
{
    public string OriginalFileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = "application/octet-stream";
    public long Length { get; init; }
    public Stream Content { get; init; } = Stream.Null;
}

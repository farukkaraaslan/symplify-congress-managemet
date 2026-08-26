namespace Symplify.BackOffice.Application.Features.CongressSliders.Commands;

public sealed class CongressSliderImageInputDto
{
    public string OriginalFileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = "application/octet-stream";
    public long Length { get; init; }
    public Stream Content { get; init; } = Stream.Null;
}

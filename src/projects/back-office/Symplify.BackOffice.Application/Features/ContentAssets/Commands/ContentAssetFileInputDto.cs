namespace Symplify.BackOffice.Application.Features.ContentAssets.Commands;

public sealed class ContentAssetFileInputDto
{
    public string OriginalFileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long Length { get; set; }
    public Stream Content { get; set; } = Stream.Null;
}

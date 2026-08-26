namespace Symplify.BackOffice.Application.Features.ExhibitionApplications.Commands.Create;

public sealed class ExhibitionApplicationFileInputDto
{
    public string OriginalFileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string? ContentType { get; set; }

    public long? FileSize { get; set; }
}

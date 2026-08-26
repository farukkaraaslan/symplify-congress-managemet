using Symplify.BackOffice.Application.Features.AbstractBook.Models;

namespace Symplify.BackOffice.Application.Features.FullTextBook.Models;


public sealed class FullTextBookBuildRequest
{
    public Guid CongressId { get; init; }
    public string? Culture { get; init; }
    public byte[]? CoverImageBytes { get; init; }
    public string? CoverImageContentType { get; init; }
}

public sealed class FullTextBookFileSourceDto
{
    public Guid FileId { get; init; }
    public Guid SubmissionId { get; init; }
    public string OriginalFileName { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public string? ContentType { get; init; }
    public long? FileSize { get; init; }
    public int VersionNo { get; init; }
}

public sealed class FullTextBookDocumentDto
{
    public Guid SubmissionId { get; init; }
    public string SubmissionNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string OriginalFileName { get; init; } = string.Empty;
    public byte[] Content { get; init; } = Array.Empty<byte>();
}

public sealed class FullTextBookDocumentModel
{
    public AbstractBookDocumentModel BaseBook { get; init; } = new();
    public IReadOnlyList<FullTextBookDocumentDto> FullTextDocuments { get; init; }
        = Array.Empty<FullTextBookDocumentDto>();
}

public sealed record FullTextBookFileResponse(byte[] Content, string FileName);

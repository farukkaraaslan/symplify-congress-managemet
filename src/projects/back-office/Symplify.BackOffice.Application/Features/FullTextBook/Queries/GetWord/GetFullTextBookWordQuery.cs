using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.FullTextBook.Constants;
using Symplify.BackOffice.Application.Features.FullTextBook.Models;
using Symplify.BackOffice.Application.Features.FullTextBook.Services;

namespace Symplify.BackOffice.Application.Features.FullTextBook.Queries.GetWord;

public sealed class GetFullTextBookWordQuery : IRequest<FullTextBookFileResponse>, ISecuredRequest
{
    public Guid CongressId { get; set; }
    public string? Culture { get; set; }
    public byte[]? CoverImageBytes { get; set; }
    public string? CoverImageContentType { get; set; }
    public string[] Roles => FullTextBookOperationClaims.AdminOnly;

    public sealed class Handler : IRequestHandler<GetFullTextBookWordQuery, FullTextBookFileResponse>
    {
        private readonly IFullTextBookDocumentBuilder _builder;
        private readonly IFullTextBookWordRenderer _renderer;

        public Handler(
            IFullTextBookDocumentBuilder builder,
            IFullTextBookWordRenderer renderer)
        {
            _builder = builder;
            _renderer = renderer;
        }

        public async Task<FullTextBookFileResponse> Handle(
            GetFullTextBookWordQuery request,
            CancellationToken cancellationToken)
        {
            FullTextBookDocumentModel model = await _builder.BuildAsync(
                new FullTextBookBuildRequest
                {
                    CongressId = request.CongressId,
                    Culture = request.Culture,
                    CoverImageBytes = request.CoverImageBytes,
                    CoverImageContentType = request.CoverImageContentType
                },
                cancellationToken);

            byte[] content = _renderer.Render(model, request.Culture);

            return new FullTextBookFileResponse(
                content,
                BuildFileName(model.BaseBook.CongressName));
        }
    }

    private static string BuildFileName(string congressName)
    {
        string safe = new string((congressName ?? "kongre")
            .Trim()
            .Select(character => char.IsLetterOrDigit(character) || character is '-' or '_'
                ? character
                : '-')
            .ToArray());

        while (safe.Contains("--", StringComparison.Ordinal))
            safe = safe.Replace("--", "-", StringComparison.Ordinal);

        safe = safe.Trim('-');
        if (string.IsNullOrWhiteSpace(safe))
            safe = "kongre";
        if (safe.Length > 80)
            safe = safe[..80].TrimEnd('-');

        return $"{safe}-tam-metin-kitabi.docx";
    }
}

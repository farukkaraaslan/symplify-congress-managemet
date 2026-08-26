using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.AbstractBook.Constants;
using Symplify.BackOffice.Application.Features.AbstractBook.Models;
using Symplify.BackOffice.Application.Features.AbstractBook.Services;
using Symplify.BackOffice.Application.Features.ProgramManagement.Models;

namespace Symplify.BackOffice.Application.Features.AbstractBook.Queries.GetWord;

public sealed class GetAbstractBookWordQuery : IRequest<AbstractBookFileResponse>, ISecuredRequest
{
    public Guid CongressId { get; set; }
    public string? Culture { get; set; }
    public ProgramSubmissionFilterDto Filter { get; set; } = new();
    public AbstractBookOptionsDto Options { get; set; } = new();
    public string[] Roles => AbstractBookOperationClaims.AdminOnly;

    public sealed class Handler : IRequestHandler<GetAbstractBookWordQuery, AbstractBookFileResponse>
    {
        private readonly IAbstractBookDocumentBuilder _builder;
        private readonly IAbstractBookWordRenderer _renderer;

        public Handler(
            IAbstractBookDocumentBuilder builder,
            IAbstractBookWordRenderer renderer)
        {
            _builder = builder;
            _renderer = renderer;
        }

        public async Task<AbstractBookFileResponse> Handle(
            GetAbstractBookWordQuery request,
            CancellationToken cancellationToken)
        {
            AbstractBookDocumentModel model = await _builder.BuildAsync(
                new AbstractBookBuildRequest
                {
                    CongressId = request.CongressId,
                    Culture = request.Culture,
                    Filter = request.Filter,
                    Options = request.Options
                },
                cancellationToken);

            byte[] content = _renderer.Render(model, request.Culture);
            return new AbstractBookFileResponse(content, BuildFileName(model.CongressName));
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

        return $"{safe}-ozet-kitabi.docx";
    }
}

using System.Text.Json;
using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.ProgramManagement.Constants;
using Symplify.BackOffice.Application.Features.ProgramManagement.Models;
using Symplify.BackOffice.Application.Features.ProgramManagement.Services;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.ProgramManagement.Queries.GetDraftPdf;

public sealed class GetProgramDraftPdfQuery : IRequest<ProgramDraftPdfResponse>, ISecuredRequest
{
    public Guid CongressId { get; set; }
    public string? Culture { get; set; }
    public ProgramBookCoverDto Cover { get; set; } = new();
    public ProgramBookRenderOptionsDto Options { get; set; } = new();
    public string? PublicBaseUrl { get; set; }
    public string[] Roles => ProgramManagementOperationClaims.AdminOnly;

    public sealed class Handler : IRequestHandler<GetProgramDraftPdfQuery, ProgramDraftPdfResponse>
    {
        private readonly IProgramManagementRepository _repository;
        private readonly IProgramDraftPdfRenderer _renderer;

        public Handler(
            IProgramManagementRepository repository,
            IProgramDraftPdfRenderer renderer)
        {
            _repository = repository;
            _renderer = renderer;
        }

        public async Task<ProgramDraftPdfResponse> Handle(
            GetProgramDraftPdfQuery request,
            CancellationToken cancellationToken)
        {
            CongressProgramPlan plan = await _repository.GetPlanForDisplayAsync(
                request.CongressId,
                cancellationToken)
                ?? throw new InvalidOperationException("PDF oluşturmak için program taslağı bulunamadı.");

            ProgramGenerationSourceDto source = await _repository.GetGenerationSourceAsync(
                request.CongressId,
                null,
                request.Culture,
                cancellationToken,
                BuildDisplayFilter(plan))
                ?? throw new InvalidOperationException("Aktif kongre bulunamadı.");

            ProgramPlanDto dto = ProgramPlanMapper.Map(plan, source);
            byte[] content = _renderer.Render(
                source.CongressName,
                dto,
                request.Culture,
                request.Cover,
                request.Options,
                request.PublicBaseUrl);
            string fileName = BuildFileName(source.CongressName);

            return new ProgramDraftPdfResponse(content, fileName);
        }

        private static ProgramSubmissionFilterDto BuildDisplayFilter(CongressProgramPlan plan)
        {
            ProgramSubmissionFilterDto savedFilter = DeserializeSavedFilter(plan.SubmissionFilterJson);
            IReadOnlyCollection<Guid> includedSubmissionIds = DeserializeEligibleSubmissionIds(
                plan.EligibleSubmissionIdsJson);

            return new ProgramSubmissionFilterDto
            {
                Preset = savedFilter.Preset,
                WorkflowStatusCodes = savedFilter.WorkflowStatusCodes,
                PaymentStatusIds = savedFilter.PaymentStatusIds,
                SubmissionTypeIds = savedFilter.SubmissionTypeIds,
                TopicIds = savedFilter.TopicIds,
                IncludedSubmissionIds = includedSubmissionIds,
                SearchText = savedFilter.SearchText
            };
        }

        private static ProgramSubmissionFilterDto DeserializeSavedFilter(string? json)
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    return JsonSerializer.Deserialize<ProgramSubmissionFilterDto>(json)
                           ?? new ProgramSubmissionFilterDto
                           {
                               Preset = ProgramSubmissionScopePreset.AcceptedOnly
                           };
                }
                catch (JsonException)
                {
                }
            }

            return new ProgramSubmissionFilterDto
            {
                Preset = ProgramSubmissionScopePreset.AcceptedOnly
            };
        }

        private static IReadOnlyCollection<Guid> DeserializeEligibleSubmissionIds(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return Array.Empty<Guid>();

            try
            {
                return (JsonSerializer.Deserialize<Guid[]>(json) ?? Array.Empty<Guid>())
                    .Where(id => id != Guid.Empty)
                    .Distinct()
                    .ToArray();
            }
            catch (JsonException)
            {
                return Array.Empty<Guid>();
            }
        }

        private static string BuildFileName(string congressName)
        {
            string safe = new string((congressName ?? "kongre-programi")
                .Trim()
                .Select(character => char.IsLetterOrDigit(character) || character is '-' or '_'
                    ? character
                    : '-')
                .ToArray());

            while (safe.Contains("--", StringComparison.Ordinal))
                safe = safe.Replace("--", "-", StringComparison.Ordinal);

            safe = safe.Trim('-');
            if (string.IsNullOrWhiteSpace(safe))
                safe = "kongre-programi";
            if (safe.Length > 80)
                safe = safe[..80].TrimEnd('-');

            return $"{safe}-taslak-program.pdf";
        }
    }
}

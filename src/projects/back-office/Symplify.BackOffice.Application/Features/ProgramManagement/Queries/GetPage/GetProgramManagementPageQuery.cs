using System.Text.Json;
using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.ProgramManagement.Constants;
using Symplify.BackOffice.Application.Features.ProgramManagement.Models;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.ProgramManagement.Queries.GetPage;

public sealed class GetProgramManagementPageQuery : IRequest<ProgramManagementPageResponse>, ISecuredRequest
{
    public Guid? CongressId { get; set; }
    public string? Culture { get; set; }
    public string[] Roles => ProgramManagementOperationClaims.AdminOnly;

    public sealed class Handler : IRequestHandler<GetProgramManagementPageQuery, ProgramManagementPageResponse>
    {
        private readonly IProgramManagementRepository _repository;

        public Handler(IProgramManagementRepository repository)
        {
            _repository = repository;
        }

        public async Task<ProgramManagementPageResponse> Handle(
            GetProgramManagementPageQuery request,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<ProgramCongressOptionDto> congresses = await _repository.GetCongressOptionsAsync(
                request.Culture,
                cancellationToken);

            Guid? selectedCongressId = request.CongressId.HasValue
                                       && request.CongressId.Value != Guid.Empty
                                       && congresses.Any(x => x.Id == request.CongressId.Value)
                ? request.CongressId.Value
                : congresses.FirstOrDefault()?.Id;

            if (!selectedCongressId.HasValue)
            {
                return new ProgramManagementPageResponse
                {
                    Congresses = congresses,
                    SelectedCongressId = null
                };
            }

            CongressProgramPlan? plan = await _repository.GetPlanForDisplayAsync(
                selectedCongressId.Value,
                cancellationToken);

            ProgramSubmissionFilterDto? displayFilter = plan is null
                ? null
                : BuildDisplayFilter(plan);

            ProgramGenerationSourceDto? source = await _repository.GetGenerationSourceAsync(
                selectedCongressId.Value,
                null,
                request.Culture,
                cancellationToken,
                displayFilter);

            return new ProgramManagementPageResponse
            {
                Congresses = congresses,
                SelectedCongressId = selectedCongressId,
                Source = source,
                Plan = plan is null || source is null ? null : ProgramPlanMapper.Map(plan, source)
            };
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
    }
}

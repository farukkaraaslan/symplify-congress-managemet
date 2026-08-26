using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.AbstractBook.Constants;
using Symplify.BackOffice.Application.Features.AbstractBook.Models;
using Symplify.BackOffice.Application.Features.ProgramManagement.Models;
using Symplify.BackOffice.Application.Services.Repositories;

namespace Symplify.BackOffice.Application.Features.AbstractBook.Queries.GetPage;

public sealed class GetAbstractBookPageQuery : IRequest<AbstractBookPageResponse>, ISecuredRequest
{
    public Guid? CongressId { get; set; }
    public string? Culture { get; set; }
    public string[] Roles => AbstractBookOperationClaims.AdminOnly;

    public sealed class Handler : IRequestHandler<GetAbstractBookPageQuery, AbstractBookPageResponse>
    {
        private readonly IProgramManagementRepository _programRepository;
        private readonly IAbstractBookRepository _abstractBookRepository;

        public Handler(
            IProgramManagementRepository programRepository,
            IAbstractBookRepository abstractBookRepository)
        {
            _programRepository = programRepository;
            _abstractBookRepository = abstractBookRepository;
        }

        public async Task<AbstractBookPageResponse> Handle(
            GetAbstractBookPageQuery request,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<ProgramCongressOptionDto> congresses = await _programRepository.GetCongressOptionsAsync(
                request.Culture,
                cancellationToken);

            Guid? selectedCongressId = request.CongressId.HasValue
                                       && request.CongressId.Value != Guid.Empty
                                       && congresses.Any(x => x.Id == request.CongressId.Value)
                ? request.CongressId.Value
                : congresses.FirstOrDefault()?.Id;

            if (!selectedCongressId.HasValue)
            {
                return new AbstractBookPageResponse
                {
                    Congresses = congresses
                };
            }

            ProgramGenerationSourceDto? source = await _programRepository.GetGenerationSourceAsync(
                selectedCongressId.Value,
                null,
                request.Culture,
                cancellationToken,
                new ProgramSubmissionFilterDto
                {
                    Preset = ProgramSubmissionScopePreset.AllActive
                });

            string? congressLogoUrl = await _abstractBookRepository.GetCongressLogoUrlAsync(
                selectedCongressId.Value,
                request.Culture,
                cancellationToken);

            return new AbstractBookPageResponse
            {
                Congresses = congresses,
                SelectedCongressId = selectedCongressId,
                Source = source,
                CongressLogoUrl = congressLogoUrl
            };
        }
    }
}

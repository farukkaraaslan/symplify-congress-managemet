using MediatR;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.Congresses.Queries.GetCloneSources;

public sealed class GetCongressCloneSourceListQuery
    : IRequest<IReadOnlyList<GetCongressCloneSourceListItemDto>>
{
    public Guid? OrganizationId { get; init; }

    public sealed class Handler
        : IRequestHandler<GetCongressCloneSourceListQuery, IReadOnlyList<GetCongressCloneSourceListItemDto>>
    {
        private readonly ICongressRepository _congressRepository;

        public Handler(ICongressRepository congressRepository)
        {
            _congressRepository = congressRepository;
        }

        public async Task<IReadOnlyList<GetCongressCloneSourceListItemDto>> Handle(
            GetCongressCloneSourceListQuery request,
            CancellationToken cancellationToken)
        {
            IQueryable<Congress> query = _congressRepository
                .Query()
                .AsNoTracking()
                .Where(congress => congress.DeletedDate == null);

            if (request.OrganizationId.HasValue && request.OrganizationId.Value != Guid.Empty)
            {
                query = query.Where(congress =>
                    congress.OrganizationId == request.OrganizationId.Value);
            }

            return await query
                .OrderByDescending(congress => congress.StartDate)
                .ThenByDescending(congress => congress.EditionNumber)
                .ThenByDescending(congress => congress.CreatedDate)
                .Select(congress => new GetCongressCloneSourceListItemDto
                {
                    Id = congress.Id,
                    OrganizationId = congress.OrganizationId,
                    Code = congress.Code,
                    Name = congress.Name,
                    EditionNumber = congress.EditionNumber,
                    StartDate = congress.StartDate,
                    EndDate = congress.EndDate,
                    Status = congress.Status
                })
                .ToListAsync(cancellationToken);
        }
    }
}

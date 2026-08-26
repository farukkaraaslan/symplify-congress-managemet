using Core.Application.Pipelines.Authorization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Features.BulkEmails.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.BulkEmails.Queries.GetComposePage;

public sealed class GetBulkEmailComposePageQuery : IRequest<GetBulkEmailComposePageResponse>, ISecuredRequest
{
    public Guid? CurrentUserId { get; set; }

    public bool IsSuperAdmin { get; set; }

    public string? Culture { get; set; }

    public Guid? SelectedCongressId { get; set; }

    public string[] Roles => [BulkEmailsOperationClaims.Admin, BulkEmailsOperationClaims.Read];

    public sealed class Handler : IRequestHandler<GetBulkEmailComposePageQuery, GetBulkEmailComposePageResponse>
    {
        private readonly ICongressRepository _congressRepository;
        private readonly IOrganizationUserRepository _organizationUserRepository;

        public Handler(
            ICongressRepository congressRepository,
            IOrganizationUserRepository organizationUserRepository)
        {
            _congressRepository = congressRepository;
            _organizationUserRepository = organizationUserRepository;
        }

        public async Task<GetBulkEmailComposePageResponse> Handle(
            GetBulkEmailComposePageQuery request,
            CancellationToken cancellationToken)
        {
            IQueryable<Congress> congressQuery = _congressRepository
                .Query()
                .AsNoTracking()
                .Include(congress => congress.Translations)
                    .ThenInclude(translation => translation.Language)
                .Where(congress =>
                    congress.DeletedDate == null &&
                    congress.Status == CongressStatus.Published);

            List<Guid> organizationIds = new();
            Guid? defaultCongressId = null;

            if (!request.IsSuperAdmin)
            {
                if (!request.CurrentUserId.HasValue || request.CurrentUserId.Value == Guid.Empty)
                {
                    return new GetBulkEmailComposePageResponse();
                }

                var memberships = await _organizationUserRepository
                    .Query()
                    .AsNoTracking()
                    .Where(item =>
                        item.UserId == request.CurrentUserId.Value &&
                        item.IsActive &&
                        item.DeletedDate == null)
                    .Select(item => new
                    {
                        item.OrganizationId,
                        item.DefaultCongressId
                    })
                    .ToListAsync(cancellationToken);

                organizationIds = memberships
                    .Select(item => item.OrganizationId)
                    .Distinct()
                    .ToList();

                defaultCongressId = memberships
                    .Select(item => item.DefaultCongressId)
                    .FirstOrDefault(id => id.HasValue && id.Value != Guid.Empty);

                congressQuery = congressQuery.Where(congress => organizationIds.Contains(congress.OrganizationId));
            }

            List<Congress> congresses = await congressQuery
                .OrderByDescending(congress => congress.StartDate)
                .ThenBy(congress => congress.Name)
                .ToListAsync(cancellationToken);

            string normalizedCulture = string.IsNullOrWhiteSpace(request.Culture)
                ? "tr-TR"
                : request.Culture.Trim();

            List<BulkEmailCongressOptionDto> options = congresses
                .Select(congress => new BulkEmailCongressOptionDto
                {
                    Id = congress.Id,
                    Text = BuildCongressText(congress, normalizedCulture)
                })
                .ToList();

            Guid? selectedCongressId = request.SelectedCongressId.HasValue &&
                                       options.Any(option => option.Id == request.SelectedCongressId.Value)
                ? request.SelectedCongressId
                : defaultCongressId.HasValue && options.Any(option => option.Id == defaultCongressId.Value)
                    ? defaultCongressId
                    : options.FirstOrDefault()?.Id;

            return new GetBulkEmailComposePageResponse
            {
                SelectedCongressId = selectedCongressId,
                Congresses = options
            };
        }

        private static string BuildCongressText(Congress congress, string culture)
        {
            string? localizedTitle = congress.Translations
                .Where(translation =>
                    translation.DeletedDate == null &&
                    translation.Language.DeletedDate == null &&
                    translation.Language.Culture.Equals(culture, StringComparison.OrdinalIgnoreCase))
                .Select(translation => translation.Title)
                .FirstOrDefault(title => !string.IsNullOrWhiteSpace(title));

            string title = localizedTitle ?? congress.Name ?? congress.Code;
            return string.IsNullOrWhiteSpace(congress.Code) ||
                   title.Contains(congress.Code, StringComparison.OrdinalIgnoreCase)
                ? title
                : $"{title} ({congress.Code})";
        }
    }
}

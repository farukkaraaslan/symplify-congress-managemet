using MediatR;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Organization;

namespace Symplify.BackOffice.Application.Features.Auth.Queries.ResolveOrganizationContext;

public sealed class ResolveAuthOrganizationContextQuery : IRequest<ResolveAuthOrganizationContextResponse?>
{
    public string? Organization { get; set; }

    public string? RequestHost { get; set; }

    public sealed class ResolveAuthOrganizationContextQueryHandler : IRequestHandler<ResolveAuthOrganizationContextQuery, ResolveAuthOrganizationContextResponse?>
    {
        private readonly IOrganizationRepository _organizationRepository;

        public ResolveAuthOrganizationContextQueryHandler(IOrganizationRepository organizationRepository)
        {
            _organizationRepository = organizationRepository;
        }

        public Task<ResolveAuthOrganizationContextResponse?> Handle(
            ResolveAuthOrganizationContextQuery request,
            CancellationToken cancellationToken)
        {
            List<Organization> organizations = _organizationRepository
                .Query()
                .Where(organization => organization.IsActive && organization.DeletedDate == null)
                .ToList();

            Organization? organization = ResolveBySlugOrCode(organizations, request.Organization)
                ?? ResolveByHost(organizations, request.RequestHost);

            return Task.FromResult(organization is null ? null : Map(organization));
        }

        private static Organization? ResolveBySlugOrCode(IEnumerable<Organization> organizations, string? value)
        {
            string? normalized = NormalizeKey(value);
            if (string.IsNullOrWhiteSpace(normalized))
                return null;

            return organizations.FirstOrDefault(organization =>
                string.Equals(organization.Id.ToString("D"), normalized, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(NormalizeKey(organization.Slug), normalized, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(NormalizeKey(organization.ShortName), normalized, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(NormalizeKey(organization.Code), normalized, StringComparison.OrdinalIgnoreCase));
        }

        private static Organization? ResolveByHost(IEnumerable<Organization> organizations, string? requestHost)
        {
            string? normalizedHost = NormalizeHost(requestHost);
            if (string.IsNullOrWhiteSpace(normalizedHost))
                return null;

            return organizations.FirstOrDefault(organization =>
                string.Equals(NormalizeHost(organization.HostUrl), normalizedHost, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(NormalizeHost(organization.WebsiteUrl), normalizedHost, StringComparison.OrdinalIgnoreCase));
        }

        private static ResolveAuthOrganizationContextResponse Map(Organization organization)
        {
            return new ResolveAuthOrganizationContextResponse
            {
                Id = organization.Id,
                Name = organization.Name,
                ShortName = organization.ShortName,
                Slug = organization.Slug,
                LogoLightPath = organization.LogoLightPath,
                LogoDarkPath = organization.LogoDarkPath
            };
        }

        private static string? NormalizeKey(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim().Trim('/').ToLowerInvariant();
        }

        private static string? NormalizeHost(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            string normalized = value.Trim();

            if (Uri.TryCreate(normalized, UriKind.Absolute, out Uri? uri))
                normalized = uri.Host;

            int slashIndex = normalized.IndexOf('/');
            if (slashIndex >= 0)
                normalized = normalized[..slashIndex];

            int portIndex = normalized.IndexOf(':');
            if (portIndex >= 0)
                normalized = normalized[..portIndex];

            normalized = normalized.Trim().Trim('.').ToLowerInvariant();

            return normalized.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
                ? normalized[4..]
                : normalized;
        }
    }
}

using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.OrganizationApiKeys.Constants;
using Symplify.BackOffice.Application.Features.OrganizationApiKeys.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Organization;

namespace Symplify.BackOffice.Application.Features.OrganizationApiKeys.Commands.Update;

public class UpdateOrganizationApiKeyCommand : IRequest<UpdatedOrganizationApiKeyResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }

    // Geriye uyumluluk için tutulur. API key farklı organizasyona taşınmaz.
    public Guid OrganizationId { get; set; }

    public string Name { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public string? Description { get; set; }
    public IReadOnlyCollection<string> Scopes { get; set; } = Array.Empty<string>();
    public string? AllowedIpAddresses { get; set; }
    public string? AllowedDomains { get; set; }
    public bool IsActive { get; set; }

    // Secret materyal ve kullanım bilgileri update edilemez; sadece mevcut eski çağrıları kırmamak için property olarak bırakıldı.
    public string? KeyPrefix { get; set; }
    public string? KeyHash { get; set; }
    public DateTime? LastUsedAt { get; set; }

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetOrganizationApiKeys";

    public string[] Roles => new[]
    {
        OrganizationApiKeysOperationClaims.Admin,
        OrganizationApiKeysOperationClaims.Write,
        OrganizationApiKeysOperationClaims.Update
    };

    public class UpdateOrganizationApiKeyCommandHandler : IRequestHandler<UpdateOrganizationApiKeyCommand, UpdatedOrganizationApiKeyResponse>
    {
        private readonly IOrganizationApiKeyRepository _repository;
        private readonly IMapper _mapper;
        private readonly OrganizationApiKeyBusinessRules _rules;

        public UpdateOrganizationApiKeyCommandHandler(
            IOrganizationApiKeyRepository repository,
            IMapper mapper,
            OrganizationApiKeyBusinessRules rules)
        {
            _repository = repository;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<UpdatedOrganizationApiKeyResponse> Handle(UpdateOrganizationApiKeyCommand request, CancellationToken cancellationToken)
        {
            await _rules.OrganizationApiKeyIdShouldBeValid(request.Id);

            OrganizationApiKey? entity = await _repository.GetAsync(
                predicate: apiKey => apiKey.Id == request.Id,
                cancellationToken: cancellationToken);

            await _rules.OrganizationApiKeyShouldExistWhenSelected(entity);
            await _rules.RevokedApiKeyShouldNotBeUpdated(entity!);
            await _rules.OrganizationShouldNotChangeWhenUpdating(entity!.OrganizationId, request.OrganizationId);

            Organization organization = await _rules.GetExistingOrganizationAsync(entity.OrganizationId, cancellationToken);
            await _rules.OrganizationShouldBeActiveWhenActivatingApiKey(organization, request.IsActive);
            await _rules.ScopesShouldBeValid(request.Scopes);
            await _rules.ExpiresAtShouldBeFutureWhenSelected(request.ExpiresAt);
            await _rules.OrganizationApiKeyNameShouldBeUniqueWhenUpdating(
                request.Id,
                entity.OrganizationId,
                request.Name,
                cancellationToken);

            entity.Name = NormalizeRequired(request.Name);
            entity.ExpiresAt = ToUtc(request.ExpiresAt);
            entity.Description = NormalizeNullable(request.Description);
            entity.Scopes = string.Join(',', NormalizeScopes(request.Scopes));
            entity.AllowedIpAddresses = NormalizeNullable(request.AllowedIpAddresses);
            entity.AllowedDomains = NormalizeNullable(request.AllowedDomains);
            entity.IsActive = request.IsActive;

            OrganizationApiKey updatedEntity = await _repository.UpdateAsync(entity);
            return _mapper.Map<UpdatedOrganizationApiKeyResponse>(updatedEntity);
        }

        private static IReadOnlyCollection<string> NormalizeScopes(IEnumerable<string>? scopes)
        {
            return (scopes ?? Array.Empty<string>())
                .Where(scope => !string.IsNullOrWhiteSpace(scope))
                .Select(scope => scope.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static DateTime? ToUtc(DateTime? value)
        {
            if (!value.HasValue)
                return null;

            DateTime dateTime = value.Value;

            return dateTime.Kind switch
            {
                DateTimeKind.Utc => dateTime,
                DateTimeKind.Local => dateTime.ToUniversalTime(),
                _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Local).ToUniversalTime()
            };
        }

        private static string NormalizeRequired(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}

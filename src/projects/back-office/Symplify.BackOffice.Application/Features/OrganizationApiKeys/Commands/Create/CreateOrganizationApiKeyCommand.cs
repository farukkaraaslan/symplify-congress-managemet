using System.Security.Cryptography;
using System.Text;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.OrganizationApiKeys.Constants;
using Symplify.BackOffice.Application.Features.OrganizationApiKeys.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Organization;

namespace Symplify.BackOffice.Application.Features.OrganizationApiKeys.Commands.Create;

public class CreateOrganizationApiKeyCommand : IRequest<CreatedOrganizationApiKeyResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Environment { get; set; } = "Production";
    public string KeyType { get; set; } = "SecretKey";
    public DateTime? ExpiresAt { get; set; }
    public string? Description { get; set; }
    public IReadOnlyCollection<string> Scopes { get; set; } = Array.Empty<string>();
    public string? AllowedIpAddresses { get; set; }
    public string? AllowedDomains { get; set; }
    public bool IsActive { get; set; } = true;

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetOrganizationApiKeys";

    public string[] Roles => new[]
    {
        OrganizationApiKeysOperationClaims.Admin,
        OrganizationApiKeysOperationClaims.Write,
        OrganizationApiKeysOperationClaims.Add
    };

    public class Handler : IRequestHandler<CreateOrganizationApiKeyCommand, CreatedOrganizationApiKeyResponse>
    {
        private readonly IOrganizationApiKeyRepository _organizationApiKeyRepository;
        private readonly OrganizationApiKeyBusinessRules _businessRules;

        public Handler(
            IOrganizationApiKeyRepository organizationApiKeyRepository,
            OrganizationApiKeyBusinessRules businessRules)
        {
            _organizationApiKeyRepository = organizationApiKeyRepository;
            _businessRules = businessRules;
        }

        public async Task<CreatedOrganizationApiKeyResponse> Handle(CreateOrganizationApiKeyCommand request, CancellationToken cancellationToken)
        {
            Organization organization = await _businessRules.GetExistingOrganizationAsync(request.OrganizationId, cancellationToken);
            await _businessRules.OrganizationShouldBeActiveWhenCreatingApiKey(organization);
            await _businessRules.EnvironmentShouldBeValid(request.Environment);
            await _businessRules.KeyTypeShouldBeValid(request.KeyType);
            await _businessRules.ScopesShouldBeValid(request.Scopes);
            await _businessRules.ExpiresAtShouldBeFutureWhenSelected(request.ExpiresAt);
            await _businessRules.OrganizationApiKeyNameShouldBeUniqueWhenCreating(
                request.OrganizationId,
                request.Name,
                cancellationToken);

            IReadOnlyCollection<string> normalizedScopes = NormalizeScopes(request.Scopes);

            string plainTextKey = Generate(request.Environment, request.KeyType);
            string keyPrefix = plainTextKey[..Math.Min(24, plainTextKey.Length)];

            var entity = new OrganizationApiKey
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                Name = NormalizeRequired(request.Name),
                Environment = NormalizeRequired(request.Environment, "Production"),
                KeyType = NormalizeRequired(request.KeyType, "SecretKey"),
                KeyPrefix = keyPrefix,
                KeyHash = Hash(plainTextKey),
                ExpiresAt = ToUtc(request.ExpiresAt),
                Description = NormalizeNullable(request.Description),
                Scopes = string.Join(',', normalizedScopes),
                AllowedIpAddresses = NormalizeNullable(request.AllowedIpAddresses),
                AllowedDomains = NormalizeNullable(request.AllowedDomains),
                IsActive = request.IsActive
            };

            OrganizationApiKey createdEntity = await _organizationApiKeyRepository.AddAsync(entity);

            return new CreatedOrganizationApiKeyResponse
            {
                Id = createdEntity.Id,
                OrganizationId = createdEntity.OrganizationId,
                Name = createdEntity.Name,
                Environment = createdEntity.Environment,
                KeyType = createdEntity.KeyType,
                KeyPrefix = createdEntity.KeyPrefix,
                PlainTextKey = plainTextKey,
                ExpiresAt = createdEntity.ExpiresAt,
                IsActive = createdEntity.IsActive
            };
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

        private static string Generate(string environment, string keyType)
        {
            string environmentPrefix = string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase)
                ? "live"
                : string.Equals(environment, "Sandbox", StringComparison.OrdinalIgnoreCase)
                    ? "test"
                    : "dev";

            string typePrefix = string.Equals(keyType, "PublicKey", StringComparison.OrdinalIgnoreCase)
                ? "pk"
                : string.Equals(keyType, "IntegrationKey", StringComparison.OrdinalIgnoreCase)
                    ? "ik"
                    : "sk";

            string secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            return $"symp_{typePrefix}_{environmentPrefix}_{secret}";
        }

        private static string Hash(string value)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
        }

        private static string NormalizeRequired(string? value, string fallback = "")
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}

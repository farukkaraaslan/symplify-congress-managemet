using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.TenantApiKeys.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Tenant;
namespace Symplify.BackOffice.Application.Features.TenantApiKeys.Commands.Create;
public class CreateTenantApiKeyCommand : IRequest<CreatedTenantApiKeyResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsActive { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetTenantApiKeys";
    public string[] Roles => new[] { TenantApiKeysOperationClaims.Admin, TenantApiKeysOperationClaims.Write, TenantApiKeysOperationClaims.Add };
    public class CreateTenantApiKeyCommandHandler : IRequestHandler<CreateTenantApiKeyCommand, CreatedTenantApiKeyResponse>
    {
        private readonly ITenantApiKeyRepository _repository;
        private readonly IMapper _mapper;
        public CreateTenantApiKeyCommandHandler(ITenantApiKeyRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<CreatedTenantApiKeyResponse> Handle(CreateTenantApiKeyCommand request, CancellationToken cancellationToken)
        {
            TenantApiKey entity = new()
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                Name = request.Name,
                KeyPrefix = request.KeyPrefix,
                KeyHash = request.KeyHash,
                ExpiresAt = request.ExpiresAt,
                LastUsedAt = request.LastUsedAt,
                IsActive = request.IsActive,
            };
            TenantApiKey createdEntity = await _repository.AddAsync(entity);
            return _mapper.Map<CreatedTenantApiKeyResponse>(createdEntity);
        }
    }
}

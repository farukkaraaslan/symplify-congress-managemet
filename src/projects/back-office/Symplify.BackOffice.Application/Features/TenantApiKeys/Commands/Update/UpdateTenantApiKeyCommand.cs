using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.TenantApiKeys.Constants;
using Symplify.BackOffice.Application.Features.TenantApiKeys.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Tenant;
namespace Symplify.BackOffice.Application.Features.TenantApiKeys.Commands.Update;
public class UpdateTenantApiKeyCommand : IRequest<UpdatedTenantApiKeyResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
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
    public string[] Roles => new[] { TenantApiKeysOperationClaims.Admin, TenantApiKeysOperationClaims.Write, TenantApiKeysOperationClaims.Update };
    public class UpdateTenantApiKeyCommandHandler : IRequestHandler<UpdateTenantApiKeyCommand, UpdatedTenantApiKeyResponse>
    {
        private readonly ITenantApiKeyRepository _repository; private readonly IMapper _mapper; private readonly TenantApiKeyBusinessRules _rules;
        public UpdateTenantApiKeyCommandHandler(ITenantApiKeyRepository repository, IMapper mapper, TenantApiKeyBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<UpdatedTenantApiKeyResponse> Handle(UpdateTenantApiKeyCommand request, CancellationToken cancellationToken)
        {
            TenantApiKey? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.TenantApiKeyShouldExistWhenSelected(entity);
            entity!.TenantId = request.TenantId;
            entity!.Name = request.Name;
            entity!.KeyPrefix = request.KeyPrefix;
            entity!.KeyHash = request.KeyHash;
            entity!.ExpiresAt = request.ExpiresAt;
            entity!.LastUsedAt = request.LastUsedAt;
            entity!.IsActive = request.IsActive;
            TenantApiKey updatedEntity = await _repository.UpdateAsync(entity!);
            return _mapper.Map<UpdatedTenantApiKeyResponse>(updatedEntity);
        }
    }
}

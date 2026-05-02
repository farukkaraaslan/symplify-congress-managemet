using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.TenantApiKeys.Constants;
using Symplify.BackOffice.Application.Features.TenantApiKeys.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Tenant;
namespace Symplify.BackOffice.Application.Features.TenantApiKeys.Commands.Delete;
public class DeleteTenantApiKeyCommand : IRequest<DeletedTenantApiKeyResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetTenantApiKeys";
    public string[] Roles => new[] { TenantApiKeysOperationClaims.Admin, TenantApiKeysOperationClaims.Write, TenantApiKeysOperationClaims.Delete };
    public class DeleteTenantApiKeyCommandHandler : IRequestHandler<DeleteTenantApiKeyCommand, DeletedTenantApiKeyResponse>
    {
        private readonly ITenantApiKeyRepository _repository; private readonly IMapper _mapper; private readonly TenantApiKeyBusinessRules _rules;
        public DeleteTenantApiKeyCommandHandler(ITenantApiKeyRepository repository, IMapper mapper, TenantApiKeyBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedTenantApiKeyResponse> Handle(DeleteTenantApiKeyCommand request, CancellationToken cancellationToken)
        {
            TenantApiKey? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.TenantApiKeyShouldExistWhenSelected(entity);
            TenantApiKey deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedTenantApiKeyResponse>(deletedEntity);
        }
    }
}

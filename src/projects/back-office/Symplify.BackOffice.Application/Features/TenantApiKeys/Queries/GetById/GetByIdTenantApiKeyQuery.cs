using AutoMapper;
using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.TenantApiKeys.Constants;
using Symplify.BackOffice.Application.Features.TenantApiKeys.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Tenant;
namespace Symplify.BackOffice.Application.Features.TenantApiKeys.Queries.GetById;
public class GetByIdTenantApiKeyQuery : IRequest<GetByIdTenantApiKeyResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { TenantApiKeysOperationClaims.Admin, TenantApiKeysOperationClaims.Read };
    public class GetByIdTenantApiKeyQueryHandler : IRequestHandler<GetByIdTenantApiKeyQuery, GetByIdTenantApiKeyResponse>
    {
        private readonly ITenantApiKeyRepository _repository; private readonly IMapper _mapper; private readonly TenantApiKeyBusinessRules _rules;
        public GetByIdTenantApiKeyQueryHandler(ITenantApiKeyRepository repository, IMapper mapper, TenantApiKeyBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<GetByIdTenantApiKeyResponse> Handle(GetByIdTenantApiKeyQuery request, CancellationToken cancellationToken)
        {
            TenantApiKey? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.TenantApiKeyShouldExistWhenSelected(entity);
            return _mapper.Map<GetByIdTenantApiKeyResponse>(entity);
        }
    }
}

using AutoMapper;
using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.Tenants.Constants;
using Symplify.BackOffice.Application.Features.Tenants.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Tenant;
namespace Symplify.BackOffice.Application.Features.Tenants.Queries.GetById;
public class GetByIdTenantQuery : IRequest<GetByIdTenantResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { TenantsOperationClaims.Admin, TenantsOperationClaims.Read };
    public class GetByIdTenantQueryHandler : IRequestHandler<GetByIdTenantQuery, GetByIdTenantResponse>
    {
        private readonly ITenantRepository _repository; private readonly IMapper _mapper; private readonly TenantBusinessRules _rules;
        public GetByIdTenantQueryHandler(ITenantRepository repository, IMapper mapper, TenantBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<GetByIdTenantResponse> Handle(GetByIdTenantQuery request, CancellationToken cancellationToken)
        {
            Tenant? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.TenantShouldExistWhenSelected(entity);
            return _mapper.Map<GetByIdTenantResponse>(entity);
        }
    }
}

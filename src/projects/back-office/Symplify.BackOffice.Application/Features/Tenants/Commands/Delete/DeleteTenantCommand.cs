using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.Tenants.Constants;
using Symplify.BackOffice.Application.Features.Tenants.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Tenant;
namespace Symplify.BackOffice.Application.Features.Tenants.Commands.Delete;
public class DeleteTenantCommand : IRequest<DeletedTenantResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetTenants";
    public string[] Roles => new[] { TenantsOperationClaims.Admin, TenantsOperationClaims.Write, TenantsOperationClaims.Delete };
    public class DeleteTenantCommandHandler : IRequestHandler<DeleteTenantCommand, DeletedTenantResponse>
    {
        private readonly ITenantRepository _repository; private readonly IMapper _mapper; private readonly TenantBusinessRules _rules;
        public DeleteTenantCommandHandler(ITenantRepository repository, IMapper mapper, TenantBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedTenantResponse> Handle(DeleteTenantCommand request, CancellationToken cancellationToken)
        {
            Tenant? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.TenantShouldExistWhenSelected(entity);
            Tenant deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedTenantResponse>(deletedEntity);
        }
    }
}

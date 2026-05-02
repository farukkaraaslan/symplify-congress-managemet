using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.Tenants.Constants;
using Symplify.BackOffice.Application.Features.Tenants.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Tenant;
namespace Symplify.BackOffice.Application.Features.Tenants.Commands.Update;
public class UpdateTenantCommand : IRequest<UpdatedTenantResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? HostUrl { get; set; }
    public bool IsActive { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetTenants";
    public string[] Roles => new[] { TenantsOperationClaims.Admin, TenantsOperationClaims.Write, TenantsOperationClaims.Update };
    public class UpdateTenantCommandHandler : IRequestHandler<UpdateTenantCommand, UpdatedTenantResponse>
    {
        private readonly ITenantRepository _repository; private readonly IMapper _mapper; private readonly TenantBusinessRules _rules;
        public UpdateTenantCommandHandler(ITenantRepository repository, IMapper mapper, TenantBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<UpdatedTenantResponse> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
        {
            Tenant? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.TenantShouldExistWhenSelected(entity);
            entity!.Name = request.Name;
            entity!.Slug = request.Slug;
            entity!.HostUrl = request.HostUrl;
            entity!.IsActive = request.IsActive;
            Tenant updatedEntity = await _repository.UpdateAsync(entity!);
            return _mapper.Map<UpdatedTenantResponse>(updatedEntity);
        }
    }
}

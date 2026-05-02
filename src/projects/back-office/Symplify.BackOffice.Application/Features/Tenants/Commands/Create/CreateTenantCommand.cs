using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.Tenants.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Tenant;
namespace Symplify.BackOffice.Application.Features.Tenants.Commands.Create;
public class CreateTenantCommand : IRequest<CreatedTenantResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? HostUrl { get; set; }
    public bool IsActive { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetTenants";
    public string[] Roles => new[] { TenantsOperationClaims.Admin, TenantsOperationClaims.Write, TenantsOperationClaims.Add };
    public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, CreatedTenantResponse>
    {
        private readonly ITenantRepository _repository;
        private readonly IMapper _mapper;
        public CreateTenantCommandHandler(ITenantRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<CreatedTenantResponse> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
        {
            Tenant entity = new()
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Slug = request.Slug,
                HostUrl = request.HostUrl,
                IsActive = request.IsActive,
            };
            Tenant createdEntity = await _repository.AddAsync(entity);
            return _mapper.Map<CreatedTenantResponse>(createdEntity);
        }
    }
}

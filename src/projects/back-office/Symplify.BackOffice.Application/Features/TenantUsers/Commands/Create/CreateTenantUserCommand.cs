using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.TenantUsers.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Tenant;
namespace Symplify.BackOffice.Application.Features.TenantUsers.Commands.Create;
public class CreateTenantUserCommand : IRequest<CreatedTenantUserResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid? DefaultCongressId { get; set; }
    public bool IsActive { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetTenantUsers";
    public string[] Roles => new[] { TenantUsersOperationClaims.Admin, TenantUsersOperationClaims.Write, TenantUsersOperationClaims.Add };
    public class CreateTenantUserCommandHandler : IRequestHandler<CreateTenantUserCommand, CreatedTenantUserResponse>
    {
        private readonly ITenantUserRepository _repository;
        private readonly IMapper _mapper;
        public CreateTenantUserCommandHandler(ITenantUserRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<CreatedTenantUserResponse> Handle(CreateTenantUserCommand request, CancellationToken cancellationToken)
        {
            TenantUser entity = new()
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                UserId = request.UserId,
                DefaultCongressId = request.DefaultCongressId,
                IsActive = request.IsActive,
            };
            TenantUser createdEntity = await _repository.AddAsync(entity);
            return _mapper.Map<CreatedTenantUserResponse>(createdEntity);
        }
    }
}

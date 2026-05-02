using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.TenantUsers.Constants;
using Symplify.BackOffice.Application.Features.TenantUsers.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Tenant;
namespace Symplify.BackOffice.Application.Features.TenantUsers.Commands.Update;
public class UpdateTenantUserCommand : IRequest<UpdatedTenantUserResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid? DefaultCongressId { get; set; }
    public bool IsActive { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetTenantUsers";
    public string[] Roles => new[] { TenantUsersOperationClaims.Admin, TenantUsersOperationClaims.Write, TenantUsersOperationClaims.Update };
    public class UpdateTenantUserCommandHandler : IRequestHandler<UpdateTenantUserCommand, UpdatedTenantUserResponse>
    {
        private readonly ITenantUserRepository _repository; private readonly IMapper _mapper; private readonly TenantUserBusinessRules _rules;
        public UpdateTenantUserCommandHandler(ITenantUserRepository repository, IMapper mapper, TenantUserBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<UpdatedTenantUserResponse> Handle(UpdateTenantUserCommand request, CancellationToken cancellationToken)
        {
            TenantUser? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.TenantUserShouldExistWhenSelected(entity);
            entity!.TenantId = request.TenantId;
            entity!.UserId = request.UserId;
            entity!.DefaultCongressId = request.DefaultCongressId;
            entity!.IsActive = request.IsActive;
            TenantUser updatedEntity = await _repository.UpdateAsync(entity!);
            return _mapper.Map<UpdatedTenantUserResponse>(updatedEntity);
        }
    }
}

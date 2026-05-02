using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.TenantUsers.Constants;
using Symplify.BackOffice.Application.Features.TenantUsers.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Tenant;
namespace Symplify.BackOffice.Application.Features.TenantUsers.Commands.Delete;
public class DeleteTenantUserCommand : IRequest<DeletedTenantUserResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetTenantUsers";
    public string[] Roles => new[] { TenantUsersOperationClaims.Admin, TenantUsersOperationClaims.Write, TenantUsersOperationClaims.Delete };
    public class DeleteTenantUserCommandHandler : IRequestHandler<DeleteTenantUserCommand, DeletedTenantUserResponse>
    {
        private readonly ITenantUserRepository _repository; private readonly IMapper _mapper; private readonly TenantUserBusinessRules _rules;
        public DeleteTenantUserCommandHandler(ITenantUserRepository repository, IMapper mapper, TenantUserBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedTenantUserResponse> Handle(DeleteTenantUserCommand request, CancellationToken cancellationToken)
        {
            TenantUser? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.TenantUserShouldExistWhenSelected(entity);
            TenantUser deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedTenantUserResponse>(deletedEntity);
        }
    }
}

using AutoMapper;
using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.TenantUsers.Constants;
using Symplify.BackOffice.Application.Features.TenantUsers.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Tenant;
namespace Symplify.BackOffice.Application.Features.TenantUsers.Queries.GetById;
public class GetByIdTenantUserQuery : IRequest<GetByIdTenantUserResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { TenantUsersOperationClaims.Admin, TenantUsersOperationClaims.Read };
    public class GetByIdTenantUserQueryHandler : IRequestHandler<GetByIdTenantUserQuery, GetByIdTenantUserResponse>
    {
        private readonly ITenantUserRepository _repository; private readonly IMapper _mapper; private readonly TenantUserBusinessRules _rules;
        public GetByIdTenantUserQueryHandler(ITenantUserRepository repository, IMapper mapper, TenantUserBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<GetByIdTenantUserResponse> Handle(GetByIdTenantUserQuery request, CancellationToken cancellationToken)
        {
            TenantUser? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.TenantUserShouldExistWhenSelected(entity);
            return _mapper.Map<GetByIdTenantUserResponse>(entity);
        }
    }
}

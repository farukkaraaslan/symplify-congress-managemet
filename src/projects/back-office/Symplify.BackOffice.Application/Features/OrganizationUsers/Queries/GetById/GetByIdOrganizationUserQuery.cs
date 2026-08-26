using AutoMapper;
using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.OrganizationUsers.Constants;
using Symplify.BackOffice.Application.Features.OrganizationUsers.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Organization;
namespace Symplify.BackOffice.Application.Features.OrganizationUsers.Queries.GetById;
public class GetByIdOrganizationUserQuery : IRequest<GetByIdOrganizationUserResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { OrganizationUsersOperationClaims.Admin, OrganizationUsersOperationClaims.Read };
    public class GetByIdOrganizationUserQueryHandler : IRequestHandler<GetByIdOrganizationUserQuery, GetByIdOrganizationUserResponse>
    {
        private readonly IOrganizationUserRepository _repository; private readonly IMapper _mapper; private readonly OrganizationUserBusinessRules _rules;
        public GetByIdOrganizationUserQueryHandler(IOrganizationUserRepository repository, IMapper mapper, OrganizationUserBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<GetByIdOrganizationUserResponse> Handle(GetByIdOrganizationUserQuery request, CancellationToken cancellationToken)
        {
            OrganizationUser? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.OrganizationUserShouldExistWhenSelected(entity);
            return _mapper.Map<GetByIdOrganizationUserResponse>(entity);
        }
    }
}

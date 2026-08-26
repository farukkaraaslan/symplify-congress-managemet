using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.OrganizationUsers.Constants;
using Symplify.BackOffice.Application.Features.OrganizationUsers.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Organization;
namespace Symplify.BackOffice.Application.Features.OrganizationUsers.Commands.Delete;
public class DeleteOrganizationUserCommand : IRequest<DeletedOrganizationUserResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetOrganizationUsers";
    public string[] Roles => new[] { OrganizationUsersOperationClaims.Admin, OrganizationUsersOperationClaims.Write, OrganizationUsersOperationClaims.Delete };
    public class DeleteOrganizationUserCommandHandler : IRequestHandler<DeleteOrganizationUserCommand, DeletedOrganizationUserResponse>
    {
        private readonly IOrganizationUserRepository _repository; private readonly IMapper _mapper; private readonly OrganizationUserBusinessRules _rules;
        public DeleteOrganizationUserCommandHandler(IOrganizationUserRepository repository, IMapper mapper, OrganizationUserBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedOrganizationUserResponse> Handle(DeleteOrganizationUserCommand request, CancellationToken cancellationToken)
        {
            OrganizationUser? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.OrganizationUserShouldExistWhenSelected(entity);
            OrganizationUser deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedOrganizationUserResponse>(deletedEntity);
        }
    }
}

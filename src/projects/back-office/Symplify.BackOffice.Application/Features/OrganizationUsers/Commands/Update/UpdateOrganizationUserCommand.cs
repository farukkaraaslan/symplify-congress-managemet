using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.OrganizationUsers.Constants;
using Symplify.BackOffice.Application.Features.OrganizationUsers.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Organization;
namespace Symplify.BackOffice.Application.Features.OrganizationUsers.Commands.Update;
public class UpdateOrganizationUserCommand : IRequest<UpdatedOrganizationUserResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public Guid? DefaultCongressId { get; set; }
    public bool IsActive { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetOrganizationUsers";
    public string[] Roles => new[] { OrganizationUsersOperationClaims.Admin, OrganizationUsersOperationClaims.Write, OrganizationUsersOperationClaims.Update };
    public class UpdateOrganizationUserCommandHandler : IRequestHandler<UpdateOrganizationUserCommand, UpdatedOrganizationUserResponse>
    {
        private readonly IOrganizationUserRepository _repository; private readonly IMapper _mapper; private readonly OrganizationUserBusinessRules _rules;
        public UpdateOrganizationUserCommandHandler(IOrganizationUserRepository repository, IMapper mapper, OrganizationUserBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<UpdatedOrganizationUserResponse> Handle(UpdateOrganizationUserCommand request, CancellationToken cancellationToken)
        {
            OrganizationUser? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.OrganizationUserShouldExistWhenSelected(entity);
            entity!.OrganizationId = request.OrganizationId;
            entity!.UserId = request.UserId;
            entity!.DefaultCongressId = request.DefaultCongressId;
            entity!.IsActive = request.IsActive;
            OrganizationUser updatedEntity = await _repository.UpdateAsync(entity!);
            return _mapper.Map<UpdatedOrganizationUserResponse>(updatedEntity);
        }
    }
}

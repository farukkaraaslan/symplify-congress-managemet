using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.OrganizationUsers.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Organization;
namespace Symplify.BackOffice.Application.Features.OrganizationUsers.Commands.Create;
public class CreateOrganizationUserCommand : IRequest<CreatedOrganizationUserResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public Guid? DefaultCongressId { get; set; }
    public bool IsActive { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetOrganizationUsers";
    public string[] Roles => new[] { OrganizationUsersOperationClaims.Admin, OrganizationUsersOperationClaims.Write, OrganizationUsersOperationClaims.Add };
    public class CreateOrganizationUserCommandHandler : IRequestHandler<CreateOrganizationUserCommand, CreatedOrganizationUserResponse>
    {
        private readonly IOrganizationUserRepository _repository;
        private readonly IMapper _mapper;
        public CreateOrganizationUserCommandHandler(IOrganizationUserRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<CreatedOrganizationUserResponse> Handle(CreateOrganizationUserCommand request, CancellationToken cancellationToken)
        {
            OrganizationUser entity = new()
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                UserId = request.UserId,
                DefaultCongressId = request.DefaultCongressId,
                IsActive = request.IsActive,
            };
            OrganizationUser createdEntity = await _repository.AddAsync(entity);
            return _mapper.Map<CreatedOrganizationUserResponse>(createdEntity);
        }
    }
}

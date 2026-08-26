using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using Core.Persistence.Paging;
using MediatR;
using Symplify.BackOffice.Application.Features.OrganizationUsers.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Organization;
namespace Symplify.BackOffice.Application.Features.OrganizationUsers.Queries.GetList;
public class GetListOrganizationUserQuery : IRequest<GetListResponse<GetListOrganizationUserListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public string[] Roles => new[] { OrganizationUsersOperationClaims.Admin, OrganizationUsersOperationClaims.Read };
    public bool BypassCache { get; }
    public string CacheKey => $"GetListOrganizationUsers({PageRequest.Page},{PageRequest.PageSize})";
    public string CacheGroupKey => "GetOrganizationUsers";
    public TimeSpan? SlidingExpiration { get; }
    public class GetListOrganizationUserQueryHandler : IRequestHandler<GetListOrganizationUserQuery, GetListResponse<GetListOrganizationUserListItemDto>>
    {
        private readonly IOrganizationUserRepository _repository; private readonly IMapper _mapper;
        public GetListOrganizationUserQueryHandler(IOrganizationUserRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<GetListResponse<GetListOrganizationUserListItemDto>> Handle(GetListOrganizationUserQuery request, CancellationToken cancellationToken)
        {
            IPaginate<OrganizationUser> entities = await _repository.GetListAsync(index: request.PageRequest.Page, size: request.PageRequest.PageSize, cancellationToken: cancellationToken);
            return _mapper.Map<GetListResponse<GetListOrganizationUserListItemDto>>(entities);
        }
    }
}

using AutoMapper;
using Core.Application.Responses;
using Core.Persistence.Paging;
using Symplify.BackOffice.Application.Features.OrganizationUsers.Commands.Create;
using Symplify.BackOffice.Application.Features.OrganizationUsers.Commands.Delete;
using Symplify.BackOffice.Application.Features.OrganizationUsers.Commands.Update;
using Symplify.BackOffice.Application.Features.OrganizationUsers.Queries.GetById;
using Symplify.BackOffice.Application.Features.OrganizationUsers.Queries.GetList;
using Symplify.BackOffice.Domain.Organization;
namespace Symplify.BackOffice.Application.Features.OrganizationUsers.Profiles;
public class MappingProfiles : AutoMapper.Profile
{
    public MappingProfiles()
    {
        CreateMap<OrganizationUser, CreateOrganizationUserCommand>().ReverseMap();
        CreateMap<OrganizationUser, CreatedOrganizationUserResponse>().ReverseMap();
        CreateMap<OrganizationUser, UpdateOrganizationUserCommand>().ReverseMap();
        CreateMap<OrganizationUser, UpdatedOrganizationUserResponse>().ReverseMap();
        CreateMap<OrganizationUser, DeletedOrganizationUserResponse>().ReverseMap();
        CreateMap<OrganizationUser, GetByIdOrganizationUserResponse>().ReverseMap();
        CreateMap<OrganizationUser, GetListOrganizationUserListItemDto>().ReverseMap();
        CreateMap<IPaginate<OrganizationUser>, GetListResponse<GetListOrganizationUserListItemDto>>().ReverseMap();
    }
}

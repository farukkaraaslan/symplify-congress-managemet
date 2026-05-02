using AutoMapper;
using Core.Application.Responses;
using Core.Persistence.Paging;
using Symplify.BackOffice.Application.Features.TenantUsers.Commands.Create;
using Symplify.BackOffice.Application.Features.TenantUsers.Commands.Delete;
using Symplify.BackOffice.Application.Features.TenantUsers.Commands.Update;
using Symplify.BackOffice.Application.Features.TenantUsers.Queries.GetById;
using Symplify.BackOffice.Application.Features.TenantUsers.Queries.GetList;
using Symplify.BackOffice.Domain.Tenant;
namespace Symplify.BackOffice.Application.Features.TenantUsers.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<TenantUser, CreateTenantUserCommand>().ReverseMap();
        CreateMap<TenantUser, CreatedTenantUserResponse>().ReverseMap();
        CreateMap<TenantUser, UpdateTenantUserCommand>().ReverseMap();
        CreateMap<TenantUser, UpdatedTenantUserResponse>().ReverseMap();
        CreateMap<TenantUser, DeletedTenantUserResponse>().ReverseMap();
        CreateMap<TenantUser, GetByIdTenantUserResponse>().ReverseMap();
        CreateMap<TenantUser, GetListTenantUserListItemDto>().ReverseMap();
        CreateMap<IPaginate<TenantUser>, GetListResponse<GetListTenantUserListItemDto>>().ReverseMap();
    }
}

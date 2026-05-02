using AutoMapper;
using Core.Application.Responses;
using Core.Persistence.Paging;
using Symplify.BackOffice.Application.Features.Tenants.Commands.Create;
using Symplify.BackOffice.Application.Features.Tenants.Commands.Delete;
using Symplify.BackOffice.Application.Features.Tenants.Commands.Update;
using Symplify.BackOffice.Application.Features.Tenants.Queries.GetById;
using Symplify.BackOffice.Application.Features.Tenants.Queries.GetList;
using Symplify.BackOffice.Domain.Tenant;
namespace Symplify.BackOffice.Application.Features.Tenants.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<Tenant, CreateTenantCommand>().ReverseMap();
        CreateMap<Tenant, CreatedTenantResponse>().ReverseMap();
        CreateMap<Tenant, UpdateTenantCommand>().ReverseMap();
        CreateMap<Tenant, UpdatedTenantResponse>().ReverseMap();
        CreateMap<Tenant, DeletedTenantResponse>().ReverseMap();
        CreateMap<Tenant, GetByIdTenantResponse>().ReverseMap();
        CreateMap<Tenant, GetListTenantListItemDto>().ReverseMap();
        CreateMap<IPaginate<Tenant>, GetListResponse<GetListTenantListItemDto>>().ReverseMap();
    }
}

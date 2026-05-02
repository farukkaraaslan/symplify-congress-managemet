using AutoMapper;
using Core.Application.Responses;
using Core.Persistence.Paging;
using Symplify.BackOffice.Application.Features.TenantApiKeys.Commands.Create;
using Symplify.BackOffice.Application.Features.TenantApiKeys.Commands.Delete;
using Symplify.BackOffice.Application.Features.TenantApiKeys.Commands.Update;
using Symplify.BackOffice.Application.Features.TenantApiKeys.Queries.GetById;
using Symplify.BackOffice.Application.Features.TenantApiKeys.Queries.GetList;
using Symplify.BackOffice.Domain.Tenant;
namespace Symplify.BackOffice.Application.Features.TenantApiKeys.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<TenantApiKey, CreateTenantApiKeyCommand>().ReverseMap();
        CreateMap<TenantApiKey, CreatedTenantApiKeyResponse>().ReverseMap();
        CreateMap<TenantApiKey, UpdateTenantApiKeyCommand>().ReverseMap();
        CreateMap<TenantApiKey, UpdatedTenantApiKeyResponse>().ReverseMap();
        CreateMap<TenantApiKey, DeletedTenantApiKeyResponse>().ReverseMap();
        CreateMap<TenantApiKey, GetByIdTenantApiKeyResponse>().ReverseMap();
        CreateMap<TenantApiKey, GetListTenantApiKeyListItemDto>().ReverseMap();
        CreateMap<IPaginate<TenantApiKey>, GetListResponse<GetListTenantApiKeyListItemDto>>().ReverseMap();
    }
}

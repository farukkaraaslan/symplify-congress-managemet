using AutoMapper;
using Core.Application.Responses;
using Core.Persistence.Paging;
using Symplify.BackOffice.Application.Features.ResourceKeys.Commands.Create;
using Symplify.BackOffice.Application.Features.ResourceKeys.Commands.Delete;
using Symplify.BackOffice.Application.Features.ResourceKeys.Commands.Update;
using Symplify.BackOffice.Application.Features.ResourceKeys.Queries.GetById;
using Symplify.BackOffice.Application.Features.ResourceKeys.Queries.GetList;
using Symplify.BackOffice.Domain.Localization;
namespace Symplify.BackOffice.Application.Features.ResourceKeys.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<ResourceKey, CreateResourceKeyCommand>().ReverseMap();
        CreateMap<ResourceKey, CreatedResourceKeyResponse>().ReverseMap();
        CreateMap<ResourceKey, UpdateResourceKeyCommand>().ReverseMap();
        CreateMap<ResourceKey, UpdatedResourceKeyResponse>().ReverseMap();
        CreateMap<ResourceKey, DeletedResourceKeyResponse>().ReverseMap();
        CreateMap<ResourceKey, GetByIdResourceKeyResponse>().ReverseMap();
        CreateMap<ResourceKey, GetListResourceKeyListItemDto>().ReverseMap();
        CreateMap<IPaginate<ResourceKey>, GetListResponse<GetListResourceKeyListItemDto>>().ReverseMap();
    }
}

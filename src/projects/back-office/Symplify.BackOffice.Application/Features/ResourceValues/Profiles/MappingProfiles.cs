using AutoMapper;
using Core.Application.Responses;
using Core.Persistence.Paging;
using Symplify.BackOffice.Application.Features.ResourceValues.Commands.Create;
using Symplify.BackOffice.Application.Features.ResourceValues.Commands.Delete;
using Symplify.BackOffice.Application.Features.ResourceValues.Commands.Update;
using Symplify.BackOffice.Application.Features.ResourceValues.Queries.GetById;
using Symplify.BackOffice.Application.Features.ResourceValues.Queries.GetList;
using Symplify.BackOffice.Domain.Localization;
namespace Symplify.BackOffice.Application.Features.ResourceValues.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<ResourceValue, CreateResourceValueCommand>().ReverseMap();
        CreateMap<ResourceValue, CreatedResourceValueResponse>().ReverseMap();
        CreateMap<ResourceValue, UpdateResourceValueCommand>().ReverseMap();
        CreateMap<ResourceValue, UpdatedResourceValueResponse>().ReverseMap();
        CreateMap<ResourceValue, DeletedResourceValueResponse>().ReverseMap();
        CreateMap<ResourceValue, GetByIdResourceValueResponse>().ReverseMap();
        CreateMap<ResourceValue, GetListResourceValueListItemDto>().ReverseMap();
        CreateMap<IPaginate<ResourceValue>, GetListResponse<GetListResourceValueListItemDto>>().ReverseMap();
    }
}

using AutoMapper;
using Symplify.BackOffice.Application.Features.Regions.Commands.Create;
using Symplify.BackOffice.Application.Features.Regions.Commands.Delete;
using Symplify.BackOffice.Application.Features.Regions.Commands.Update;
using Symplify.BackOffice.Domain.Reference;
namespace Symplify.BackOffice.Application.Features.Regions.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<Region, CreatedRegionResponse>().ReverseMap();
        CreateMap<Region, UpdatedRegionResponse>().ReverseMap();
        CreateMap<Region, DeletedRegionResponse>().ReverseMap();
    }
}

using AutoMapper;
using Symplify.BackOffice.Application.Features.Cities.Commands.Create;
using Symplify.BackOffice.Application.Features.Cities.Commands.Delete;
using Symplify.BackOffice.Application.Features.Cities.Commands.Update;
using Symplify.BackOffice.Domain.Reference;
namespace Symplify.BackOffice.Application.Features.Cities.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<City, CreatedCityResponse>().ReverseMap();
        CreateMap<City, UpdatedCityResponse>().ReverseMap();
        CreateMap<City, DeletedCityResponse>().ReverseMap();
    }
}

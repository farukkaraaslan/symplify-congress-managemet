using AutoMapper;
using Symplify.BackOffice.Application.Features.Countries.Commands.Create;
using Symplify.BackOffice.Application.Features.Countries.Commands.Delete;
using Symplify.BackOffice.Application.Features.Countries.Commands.Update;
using Symplify.BackOffice.Domain.Reference;
namespace Symplify.BackOffice.Application.Features.Countries.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<Country, CreatedCountryResponse>().ReverseMap();
        CreateMap<Country, UpdatedCountryResponse>().ReverseMap();
        CreateMap<Country, DeletedCountryResponse>().ReverseMap();
    }
}

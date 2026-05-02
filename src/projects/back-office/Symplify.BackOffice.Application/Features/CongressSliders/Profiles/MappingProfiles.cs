using AutoMapper;
using Symplify.BackOffice.Application.Features.CongressSliders.Commands.Create;
using Symplify.BackOffice.Application.Features.CongressSliders.Commands.Delete;
using Symplify.BackOffice.Application.Features.CongressSliders.Commands.Update;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressSliders.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<CongressSlider, CreatedCongressSliderResponse>().ReverseMap();
        CreateMap<CongressSlider, UpdatedCongressSliderResponse>().ReverseMap();
        CreateMap<CongressSlider, DeletedCongressSliderResponse>().ReverseMap();
    }
}

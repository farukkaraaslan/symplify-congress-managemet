using AutoMapper;
using Symplify.BackOffice.Application.Features.Congresses.Commands.Create;
using Symplify.BackOffice.Application.Features.Congresses.Commands.Delete;
using Symplify.BackOffice.Application.Features.Congresses.Commands.Update;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.Congresses.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<Congress, CreatedCongressResponse>().ReverseMap();
        CreateMap<Congress, UpdatedCongressResponse>().ReverseMap();
        CreateMap<Congress, DeletedCongressResponse>().ReverseMap();
    }
}

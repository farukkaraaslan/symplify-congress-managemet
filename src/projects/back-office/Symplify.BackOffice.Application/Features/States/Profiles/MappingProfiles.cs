using AutoMapper;
using Symplify.BackOffice.Application.Features.States.Commands.Create;
using Symplify.BackOffice.Application.Features.States.Commands.Delete;
using Symplify.BackOffice.Application.Features.States.Commands.Update;
using Symplify.BackOffice.Domain.Reference;
namespace Symplify.BackOffice.Application.Features.States.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<State, CreatedStateResponse>().ReverseMap();
        CreateMap<State, UpdatedStateResponse>().ReverseMap();
        CreateMap<State, DeletedStateResponse>().ReverseMap();
    }
}

using AutoMapper;
using Symplify.BackOffice.Application.Features.EventRooms.Commands.Create;
using Symplify.BackOffice.Application.Features.EventRooms.Commands.Delete;
using Symplify.BackOffice.Application.Features.EventRooms.Commands.Update;
using Symplify.BackOffice.Domain.Lookups;
namespace Symplify.BackOffice.Application.Features.EventRooms.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<EventRoom, CreatedEventRoomResponse>().ReverseMap();
        CreateMap<EventRoom, UpdatedEventRoomResponse>().ReverseMap();
        CreateMap<EventRoom, DeletedEventRoomResponse>().ReverseMap();
    }
}

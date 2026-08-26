using AutoMapper;
using Symplify.BackOffice.Application.Features.CongressAnnouncements.Commands.Create;
using Symplify.BackOffice.Application.Features.CongressAnnouncements.Commands.Delete;
using Symplify.BackOffice.Application.Features.CongressAnnouncements.Commands.Update;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressAnnouncements.Profiles;

public class MappingProfiles : AutoMapper.Profile
{
    public MappingProfiles()
    {
        CreateMap<CongressAnnouncement, CreatedCongressAnnouncementResponse>().ReverseMap();
        CreateMap<CongressAnnouncement, UpdatedCongressAnnouncementResponse>().ReverseMap();
        CreateMap<CongressAnnouncement, DeletedCongressAnnouncementResponse>().ReverseMap();
    }
}

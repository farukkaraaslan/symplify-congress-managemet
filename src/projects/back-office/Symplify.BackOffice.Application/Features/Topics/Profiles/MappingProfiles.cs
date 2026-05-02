using AutoMapper;
using Symplify.BackOffice.Application.Features.Topics.Commands.Create;
using Symplify.BackOffice.Application.Features.Topics.Commands.Delete;
using Symplify.BackOffice.Application.Features.Topics.Commands.Update;
using Symplify.BackOffice.Domain.Lookups;
namespace Symplify.BackOffice.Application.Features.Topics.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<Topic, CreatedTopicResponse>().ReverseMap();
        CreateMap<Topic, UpdatedTopicResponse>().ReverseMap();
        CreateMap<Topic, DeletedTopicResponse>().ReverseMap();
    }
}

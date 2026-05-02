using AutoMapper;
using Core.Application.Responses;
using Core.Persistence.Paging;
using Symplify.BackOffice.Application.Features.CongressTopics.Commands.Create;
using Symplify.BackOffice.Application.Features.CongressTopics.Commands.Delete;
using Symplify.BackOffice.Application.Features.CongressTopics.Commands.Update;
using Symplify.BackOffice.Application.Features.CongressTopics.Queries.GetById;
using Symplify.BackOffice.Application.Features.CongressTopics.Queries.GetList;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressTopics.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<CongressTopic, CreateCongressTopicCommand>().ReverseMap();
        CreateMap<CongressTopic, CreatedCongressTopicResponse>().ReverseMap();
        CreateMap<CongressTopic, UpdateCongressTopicCommand>().ReverseMap();
        CreateMap<CongressTopic, UpdatedCongressTopicResponse>().ReverseMap();
        CreateMap<CongressTopic, DeletedCongressTopicResponse>().ReverseMap();
        CreateMap<CongressTopic, GetByIdCongressTopicResponse>().ReverseMap();
        CreateMap<CongressTopic, GetListCongressTopicListItemDto>().ReverseMap();
        CreateMap<IPaginate<CongressTopic>, GetListResponse<GetListCongressTopicListItemDto>>().ReverseMap();
    }
}

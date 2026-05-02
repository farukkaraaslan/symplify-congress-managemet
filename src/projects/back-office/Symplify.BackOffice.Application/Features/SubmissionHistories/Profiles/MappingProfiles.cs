using AutoMapper;
using Core.Application.Responses;
using Core.Persistence.Paging;
using Symplify.BackOffice.Application.Features.SubmissionHistories.Commands.Create;
using Symplify.BackOffice.Application.Features.SubmissionHistories.Commands.Delete;
using Symplify.BackOffice.Application.Features.SubmissionHistories.Commands.Update;
using Symplify.BackOffice.Application.Features.SubmissionHistories.Queries.GetById;
using Symplify.BackOffice.Application.Features.SubmissionHistories.Queries.GetList;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.SubmissionHistories.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<SubmissionHistory, CreateSubmissionHistoryCommand>().ReverseMap();
        CreateMap<SubmissionHistory, CreatedSubmissionHistoryResponse>().ReverseMap();
        CreateMap<SubmissionHistory, UpdateSubmissionHistoryCommand>().ReverseMap();
        CreateMap<SubmissionHistory, UpdatedSubmissionHistoryResponse>().ReverseMap();
        CreateMap<SubmissionHistory, DeletedSubmissionHistoryResponse>().ReverseMap();
        CreateMap<SubmissionHistory, GetByIdSubmissionHistoryResponse>().ReverseMap();
        CreateMap<SubmissionHistory, GetListSubmissionHistoryListItemDto>().ReverseMap();
        CreateMap<IPaginate<SubmissionHistory>, GetListResponse<GetListSubmissionHistoryListItemDto>>().ReverseMap();
    }
}

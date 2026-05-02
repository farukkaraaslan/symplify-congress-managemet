using AutoMapper;
using Core.Application.Responses;
using Core.Persistence.Paging;
using Symplify.BackOffice.Application.Features.Submissions.Commands.Create;
using Symplify.BackOffice.Application.Features.Submissions.Commands.Delete;
using Symplify.BackOffice.Application.Features.Submissions.Commands.Update;
using Symplify.BackOffice.Application.Features.Submissions.Queries.GetById;
using Symplify.BackOffice.Application.Features.Submissions.Queries.GetList;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.Submissions.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<Submission, CreateSubmissionCommand>().ReverseMap();
        CreateMap<Submission, CreatedSubmissionResponse>().ReverseMap();
        CreateMap<Submission, UpdateSubmissionCommand>().ReverseMap();
        CreateMap<Submission, UpdatedSubmissionResponse>().ReverseMap();
        CreateMap<Submission, DeletedSubmissionResponse>().ReverseMap();
        CreateMap<Submission, GetByIdSubmissionResponse>().ReverseMap();
        CreateMap<Submission, GetListSubmissionListItemDto>().ReverseMap();
        CreateMap<IPaginate<Submission>, GetListResponse<GetListSubmissionListItemDto>>().ReverseMap();
    }
}

using AutoMapper;
using Core.Application.Responses;
using Core.Persistence.Paging;
using Symplify.BackOffice.Application.Features.SubmissionEvaluations.Commands.Create;
using Symplify.BackOffice.Application.Features.SubmissionEvaluations.Commands.Delete;
using Symplify.BackOffice.Application.Features.SubmissionEvaluations.Commands.Update;
using Symplify.BackOffice.Application.Features.SubmissionEvaluations.Queries.GetById;
using Symplify.BackOffice.Application.Features.SubmissionEvaluations.Queries.GetList;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.SubmissionEvaluations.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<SubmissionEvaluation, CreateSubmissionEvaluationCommand>().ReverseMap();
        CreateMap<SubmissionEvaluation, CreatedSubmissionEvaluationResponse>().ReverseMap();
        CreateMap<SubmissionEvaluation, UpdateSubmissionEvaluationCommand>().ReverseMap();
        CreateMap<SubmissionEvaluation, UpdatedSubmissionEvaluationResponse>().ReverseMap();
        CreateMap<SubmissionEvaluation, DeletedSubmissionEvaluationResponse>().ReverseMap();
        CreateMap<SubmissionEvaluation, GetByIdSubmissionEvaluationResponse>().ReverseMap();
        CreateMap<SubmissionEvaluation, GetListSubmissionEvaluationListItemDto>().ReverseMap();
        CreateMap<IPaginate<SubmissionEvaluation>, GetListResponse<GetListSubmissionEvaluationListItemDto>>().ReverseMap();
    }
}

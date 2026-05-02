using AutoMapper;
using Core.Application.Responses;
using Core.Persistence.Paging;
using Symplify.BackOffice.Application.Features.EvaluationScores.Commands.Create;
using Symplify.BackOffice.Application.Features.EvaluationScores.Commands.Delete;
using Symplify.BackOffice.Application.Features.EvaluationScores.Commands.Update;
using Symplify.BackOffice.Application.Features.EvaluationScores.Queries.GetById;
using Symplify.BackOffice.Application.Features.EvaluationScores.Queries.GetList;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.EvaluationScores.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<EvaluationScore, CreateEvaluationScoreCommand>().ReverseMap();
        CreateMap<EvaluationScore, CreatedEvaluationScoreResponse>().ReverseMap();
        CreateMap<EvaluationScore, UpdateEvaluationScoreCommand>().ReverseMap();
        CreateMap<EvaluationScore, UpdatedEvaluationScoreResponse>().ReverseMap();
        CreateMap<EvaluationScore, DeletedEvaluationScoreResponse>().ReverseMap();
        CreateMap<EvaluationScore, GetByIdEvaluationScoreResponse>().ReverseMap();
        CreateMap<EvaluationScore, GetListEvaluationScoreListItemDto>().ReverseMap();
        CreateMap<IPaginate<EvaluationScore>, GetListResponse<GetListEvaluationScoreListItemDto>>().ReverseMap();
    }
}

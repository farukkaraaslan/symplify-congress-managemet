using AutoMapper;
using Core.Application.Responses;
using Core.Persistence.Paging;
using Symplify.BackOffice.Application.Features.CongressEvaluationCriteria.Commands.Create;
using Symplify.BackOffice.Application.Features.CongressEvaluationCriteria.Commands.Delete;
using Symplify.BackOffice.Application.Features.CongressEvaluationCriteria.Commands.Update;
using Symplify.BackOffice.Application.Features.CongressEvaluationCriteria.Queries.GetById;
using Symplify.BackOffice.Application.Features.CongressEvaluationCriteria.Queries.GetList;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressEvaluationCriteria.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<CongressEvaluationCriterion, CreateCongressEvaluationCriterionCommand>().ReverseMap();
        CreateMap<CongressEvaluationCriterion, CreatedCongressEvaluationCriterionResponse>().ReverseMap();
        CreateMap<CongressEvaluationCriterion, UpdateCongressEvaluationCriterionCommand>().ReverseMap();
        CreateMap<CongressEvaluationCriterion, UpdatedCongressEvaluationCriterionResponse>().ReverseMap();
        CreateMap<CongressEvaluationCriterion, DeletedCongressEvaluationCriterionResponse>().ReverseMap();
        CreateMap<CongressEvaluationCriterion, GetByIdCongressEvaluationCriterionResponse>().ReverseMap();
        CreateMap<CongressEvaluationCriterion, GetListCongressEvaluationCriterionListItemDto>().ReverseMap();
        CreateMap<IPaginate<CongressEvaluationCriterion>, GetListResponse<GetListCongressEvaluationCriterionListItemDto>>().ReverseMap();
    }
}

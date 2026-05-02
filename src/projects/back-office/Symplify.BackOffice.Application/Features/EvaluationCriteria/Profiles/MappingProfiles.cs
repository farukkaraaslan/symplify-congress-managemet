using AutoMapper;
using Symplify.BackOffice.Application.Features.EvaluationCriteria.Commands.Create;
using Symplify.BackOffice.Application.Features.EvaluationCriteria.Commands.Delete;
using Symplify.BackOffice.Application.Features.EvaluationCriteria.Commands.Update;
using Symplify.BackOffice.Domain.Lookups;
namespace Symplify.BackOffice.Application.Features.EvaluationCriteria.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<EvaluationCriterion, CreatedEvaluationCriterionResponse>().ReverseMap();
        CreateMap<EvaluationCriterion, UpdatedEvaluationCriterionResponse>().ReverseMap();
        CreateMap<EvaluationCriterion, DeletedEvaluationCriterionResponse>().ReverseMap();
    }
}

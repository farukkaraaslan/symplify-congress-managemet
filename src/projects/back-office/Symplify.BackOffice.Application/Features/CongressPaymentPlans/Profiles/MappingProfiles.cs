using AutoMapper;
using Symplify.BackOffice.Application.Features.CongressPaymentPlans.Commands.Create;
using Symplify.BackOffice.Application.Features.CongressPaymentPlans.Commands.Delete;
using Symplify.BackOffice.Application.Features.CongressPaymentPlans.Commands.Update;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressPaymentPlans.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<CongressPaymentPlan, CreatedCongressPaymentPlanResponse>().ReverseMap();
        CreateMap<CongressPaymentPlan, UpdatedCongressPaymentPlanResponse>().ReverseMap();
        CreateMap<CongressPaymentPlan, DeletedCongressPaymentPlanResponse>().ReverseMap();
    }
}

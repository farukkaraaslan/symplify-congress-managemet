using AutoMapper;
using Symplify.BackOffice.Application.Features.PaymentStatuses.Commands.Create;
using Symplify.BackOffice.Application.Features.PaymentStatuses.Commands.Delete;
using Symplify.BackOffice.Application.Features.PaymentStatuses.Commands.Update;
using Symplify.BackOffice.Domain.Workflow;
namespace Symplify.BackOffice.Application.Features.PaymentStatuses.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<PaymentStatus, CreatedPaymentStatusResponse>().ReverseMap();
        CreateMap<PaymentStatus, UpdatedPaymentStatusResponse>().ReverseMap();
        CreateMap<PaymentStatus, DeletedPaymentStatusResponse>().ReverseMap();
    }
}

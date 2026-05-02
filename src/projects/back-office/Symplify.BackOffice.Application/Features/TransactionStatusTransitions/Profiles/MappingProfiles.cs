using AutoMapper;
using Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Commands.Create;
using Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Commands.Delete;
using Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Commands.Update;
using Symplify.BackOffice.Domain.Workflow;
namespace Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<TransactionStatusTransition, CreatedTransactionStatusTransitionResponse>().ReverseMap();
        CreateMap<TransactionStatusTransition, UpdatedTransactionStatusTransitionResponse>().ReverseMap();
        CreateMap<TransactionStatusTransition, DeletedTransactionStatusTransitionResponse>().ReverseMap();
    }
}

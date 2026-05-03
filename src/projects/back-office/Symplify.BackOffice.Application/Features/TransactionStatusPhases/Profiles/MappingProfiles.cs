using AutoMapper;
using Symplify.BackOffice.Application.Features.TransactionStatusPhases.Commands.Create;
using Symplify.BackOffice.Application.Features.TransactionStatusPhases.Commands.Delete;
using Symplify.BackOffice.Application.Features.TransactionStatusPhases.Commands.Update;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.TransactionStatusPhases.Profiles;

public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<TransactionStatusPhase, CreatedTransactionStatusPhaseResponse>().ReverseMap();
        CreateMap<TransactionStatusPhase, UpdatedTransactionStatusPhaseResponse>().ReverseMap();
        CreateMap<TransactionStatusPhase, DeletedTransactionStatusPhaseResponse>().ReverseMap();
    }
}

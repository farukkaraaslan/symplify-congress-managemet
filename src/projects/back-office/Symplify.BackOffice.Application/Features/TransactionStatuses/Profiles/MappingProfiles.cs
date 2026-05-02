using AutoMapper;
using Symplify.BackOffice.Application.Features.TransactionStatuses.Commands.Create;
using Symplify.BackOffice.Application.Features.TransactionStatuses.Commands.Delete;
using Symplify.BackOffice.Application.Features.TransactionStatuses.Commands.Update;
using Symplify.BackOffice.Domain.Workflow;
namespace Symplify.BackOffice.Application.Features.TransactionStatuses.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<TransactionStatus, CreatedTransactionStatusResponse>().ReverseMap();
        CreateMap<TransactionStatus, UpdatedTransactionStatusResponse>().ReverseMap();
        CreateMap<TransactionStatus, DeletedTransactionStatusResponse>().ReverseMap();
    }
}

using AutoMapper;
using Core.Application.Responses;
using Core.Persistence.Paging;
using Symplify.BackOffice.Application.Features.PaymentDocuments.Commands.Create;
using Symplify.BackOffice.Application.Features.PaymentDocuments.Commands.Delete;
using Symplify.BackOffice.Application.Features.PaymentDocuments.Commands.Update;
using Symplify.BackOffice.Application.Features.PaymentDocuments.Queries.GetById;
using Symplify.BackOffice.Application.Features.PaymentDocuments.Queries.GetList;
using Symplify.BackOffice.Domain.Workflow;
namespace Symplify.BackOffice.Application.Features.PaymentDocuments.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<PaymentDocument, CreatePaymentDocumentCommand>().ReverseMap();
        CreateMap<PaymentDocument, CreatedPaymentDocumentResponse>().ReverseMap();
        CreateMap<PaymentDocument, UpdatePaymentDocumentCommand>().ReverseMap();
        CreateMap<PaymentDocument, UpdatedPaymentDocumentResponse>().ReverseMap();
        CreateMap<PaymentDocument, DeletedPaymentDocumentResponse>().ReverseMap();
        CreateMap<PaymentDocument, GetByIdPaymentDocumentResponse>().ReverseMap();
        CreateMap<PaymentDocument, GetListPaymentDocumentListItemDto>().ReverseMap();
        CreateMap<IPaginate<PaymentDocument>, GetListResponse<GetListPaymentDocumentListItemDto>>().ReverseMap();
    }
}

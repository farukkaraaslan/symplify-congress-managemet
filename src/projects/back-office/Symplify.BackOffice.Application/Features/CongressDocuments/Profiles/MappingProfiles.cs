using AutoMapper;
using Core.Application.Responses;
using Core.Persistence.Paging;
using Symplify.BackOffice.Application.Features.CongressDocuments.Commands.Create;
using Symplify.BackOffice.Application.Features.CongressDocuments.Commands.Delete;
using Symplify.BackOffice.Application.Features.CongressDocuments.Commands.Update;
using Symplify.BackOffice.Application.Features.CongressDocuments.Queries.GetById;
using Symplify.BackOffice.Application.Features.CongressDocuments.Queries.GetList;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressDocuments.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<CongressDocument, CreateCongressDocumentCommand>().ReverseMap();
        CreateMap<CongressDocument, CreatedCongressDocumentResponse>().ReverseMap();
        CreateMap<CongressDocument, UpdateCongressDocumentCommand>().ReverseMap();
        CreateMap<CongressDocument, UpdatedCongressDocumentResponse>().ReverseMap();
        CreateMap<CongressDocument, DeletedCongressDocumentResponse>().ReverseMap();
        CreateMap<CongressDocument, GetByIdCongressDocumentResponse>().ReverseMap();
        CreateMap<CongressDocument, GetListCongressDocumentListItemDto>().ReverseMap();
        CreateMap<IPaginate<CongressDocument>, GetListResponse<GetListCongressDocumentListItemDto>>().ReverseMap();
    }
}

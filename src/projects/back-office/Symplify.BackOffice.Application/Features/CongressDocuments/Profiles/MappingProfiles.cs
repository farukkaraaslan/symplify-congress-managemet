using AutoMapper;
using Symplify.BackOffice.Application.Features.CongressDocuments.Commands.Create;
using Symplify.BackOffice.Application.Features.CongressDocuments.Commands.Delete;
using Symplify.BackOffice.Application.Features.CongressDocuments.Commands.Update;
using Symplify.BackOffice.Application.Features.CongressDocuments.Queries.GetById;
using Symplify.BackOffice.Application.Features.CongressDocuments.Queries.GetForUpdate;
using Symplify.BackOffice.Application.Features.CongressDocuments.Queries.GetList;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressDocuments.Profiles;

public class MappingProfiles : AutoMapper.Profile
{
    public MappingProfiles()
    {
        CreateMap<CongressDocument, CreatedCongressDocumentResponse>();
        CreateMap<CongressDocument, UpdatedCongressDocumentResponse>();
        CreateMap<CongressDocument, DeletedCongressDocumentResponse>();
        CreateMap<CongressDocument, GetByIdCongressDocumentResponse>();
        CreateMap<CongressDocument, GetCongressDocumentForUpdateResponse>();

        CreateMap<CongressDocument, GetListCongressDocumentListItemDto>()
            .ForMember(destination => destination.DocumentTypeName, operation => operation.Ignore())
            .ForMember(destination => destination.DisplayLanguageId, operation => operation.Ignore())
            .ForMember(destination => destination.IsFallback, operation => operation.Ignore());
    }
}

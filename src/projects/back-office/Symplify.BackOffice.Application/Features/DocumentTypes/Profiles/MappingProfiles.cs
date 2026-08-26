using AutoMapper;
using Symplify.BackOffice.Application.Features.DocumentTypes.Commands.Create;
using Symplify.BackOffice.Application.Features.DocumentTypes.Commands.Delete;
using Symplify.BackOffice.Application.Features.DocumentTypes.Commands.Update;
using Symplify.BackOffice.Domain.Lookups;
namespace Symplify.BackOffice.Application.Features.DocumentTypes.Profiles;
public class MappingProfiles : AutoMapper.Profile
{
    public MappingProfiles()
    {
        CreateMap<DocumentType, CreatedDocumentTypeResponse>().ReverseMap();
        CreateMap<DocumentType, UpdatedDocumentTypeResponse>().ReverseMap();
        CreateMap<DocumentType, DeletedDocumentTypeResponse>().ReverseMap();
    }
}

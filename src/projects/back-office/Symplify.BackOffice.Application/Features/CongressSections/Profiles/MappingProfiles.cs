using AutoMapper;
using Symplify.BackOffice.Application.Features.CongressSections.Commands.Create;
using Symplify.BackOffice.Application.Features.CongressSections.Commands.Delete;
using Symplify.BackOffice.Application.Features.CongressSections.Commands.Update;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressSections.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<CongressSection, CreatedCongressSectionResponse>().ReverseMap();
        CreateMap<CongressSection, UpdatedCongressSectionResponse>().ReverseMap();
        CreateMap<CongressSection, DeletedCongressSectionResponse>().ReverseMap();
    }
}

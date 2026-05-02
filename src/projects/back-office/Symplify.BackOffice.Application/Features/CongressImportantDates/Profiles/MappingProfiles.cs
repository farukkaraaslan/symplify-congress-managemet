using AutoMapper;
using Symplify.BackOffice.Application.Features.CongressImportantDates.Commands.Create;
using Symplify.BackOffice.Application.Features.CongressImportantDates.Commands.Delete;
using Symplify.BackOffice.Application.Features.CongressImportantDates.Commands.Update;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressImportantDates.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<CongressImportantDate, CreatedCongressImportantDateResponse>().ReverseMap();
        CreateMap<CongressImportantDate, UpdatedCongressImportantDateResponse>().ReverseMap();
        CreateMap<CongressImportantDate, DeletedCongressImportantDateResponse>().ReverseMap();
    }
}

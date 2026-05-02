using AutoMapper;
using Symplify.BackOffice.Application.Features.Titles.Commands.Create;
using Symplify.BackOffice.Application.Features.Titles.Commands.Delete;
using Symplify.BackOffice.Application.Features.Titles.Commands.Update;
using Symplify.BackOffice.Domain.Lookups;
namespace Symplify.BackOffice.Application.Features.Titles.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<Title, CreatedTitleResponse>().ReverseMap();
        CreateMap<Title, UpdatedTitleResponse>().ReverseMap();
        CreateMap<Title, DeletedTitleResponse>().ReverseMap();
    }
}

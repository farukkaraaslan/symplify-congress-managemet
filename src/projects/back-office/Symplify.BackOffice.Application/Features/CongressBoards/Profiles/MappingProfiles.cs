using AutoMapper;
using Symplify.BackOffice.Application.Features.CongressBoards.Commands.Create;
using Symplify.BackOffice.Application.Features.CongressBoards.Commands.Delete;
using Symplify.BackOffice.Application.Features.CongressBoards.Commands.Update;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressBoards.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<CongressBoard, CreatedCongressBoardResponse>().ReverseMap();
        CreateMap<CongressBoard, UpdatedCongressBoardResponse>().ReverseMap();
        CreateMap<CongressBoard, DeletedCongressBoardResponse>().ReverseMap();
    }
}

using AutoMapper;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands.Create;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands.Delete;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands.Update;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressBoardMembers.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<CongressBoardMember, CreatedCongressBoardMemberResponse>().ReverseMap();
        CreateMap<CongressBoardMember, UpdatedCongressBoardMemberResponse>().ReverseMap();
        CreateMap<CongressBoardMember, DeletedCongressBoardMemberResponse>().ReverseMap();
    }
}

using AutoMapper;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands.Create;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands.Delete;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands.Update;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Queries.GetById;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressBoardMembers.Profiles;

public class MappingProfiles : AutoMapper.Profile
{
    public MappingProfiles()
    {
        CreateMap<CongressBoardMember, CreatedCongressBoardMemberResponse>();
        CreateMap<CongressBoardMember, UpdatedCongressBoardMemberResponse>();
        CreateMap<CongressBoardMember, DeletedCongressBoardMemberResponse>();
        CreateMap<CongressBoardMember, GetByIdCongressBoardMemberResponse>();
    }
}

using AutoMapper;
using Symplify.BackOffice.Application.Features.SubmissionTypes.Commands.Create;
using Symplify.BackOffice.Application.Features.SubmissionTypes.Commands.Delete;
using Symplify.BackOffice.Application.Features.SubmissionTypes.Commands.Update;
using Symplify.BackOffice.Domain.Lookups;
namespace Symplify.BackOffice.Application.Features.SubmissionTypes.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<SubmissionType, CreatedSubmissionTypeResponse>().ReverseMap();
        CreateMap<SubmissionType, UpdatedSubmissionTypeResponse>().ReverseMap();
        CreateMap<SubmissionType, DeletedSubmissionTypeResponse>().ReverseMap();
    }
}

using AutoMapper;
using Core.Application.Responses;
using Core.Persistence.Paging;
using Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Commands.Create;
using Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Commands.Delete;
using Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Commands.Update;
using Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Queries.GetById;
using Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Queries.GetList;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<CongressSubmissionType, CreateCongressSubmissionTypeCommand>().ReverseMap();
        CreateMap<CongressSubmissionType, CreatedCongressSubmissionTypeResponse>().ReverseMap();
        CreateMap<CongressSubmissionType, UpdateCongressSubmissionTypeCommand>().ReverseMap();
        CreateMap<CongressSubmissionType, UpdatedCongressSubmissionTypeResponse>().ReverseMap();
        CreateMap<CongressSubmissionType, DeletedCongressSubmissionTypeResponse>().ReverseMap();
        CreateMap<CongressSubmissionType, GetByIdCongressSubmissionTypeResponse>().ReverseMap();
        CreateMap<CongressSubmissionType, GetListCongressSubmissionTypeListItemDto>().ReverseMap();
        CreateMap<IPaginate<CongressSubmissionType>, GetListResponse<GetListCongressSubmissionTypeListItemDto>>().ReverseMap();
    }
}

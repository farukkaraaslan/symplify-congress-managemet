using AutoMapper;
using Core.Application.Responses;
using Core.Persistence.Paging;
using Symplify.BackOffice.Application.Features.Authors.Commands.Create;
using Symplify.BackOffice.Application.Features.Authors.Commands.Delete;
using Symplify.BackOffice.Application.Features.Authors.Commands.Update;
using Symplify.BackOffice.Application.Features.Authors.Queries.GetById;
using Symplify.BackOffice.Application.Features.Authors.Queries.GetList;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.Authors.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<Author, CreateAuthorCommand>().ReverseMap();
        CreateMap<Author, CreatedAuthorResponse>().ReverseMap();
        CreateMap<Author, UpdateAuthorCommand>().ReverseMap();
        CreateMap<Author, UpdatedAuthorResponse>().ReverseMap();
        CreateMap<Author, DeletedAuthorResponse>().ReverseMap();
        CreateMap<Author, GetByIdAuthorResponse>().ReverseMap();
        CreateMap<Author, GetListAuthorListItemDto>().ReverseMap();
        CreateMap<IPaginate<Author>, GetListResponse<GetListAuthorListItemDto>>().ReverseMap();
    }
}

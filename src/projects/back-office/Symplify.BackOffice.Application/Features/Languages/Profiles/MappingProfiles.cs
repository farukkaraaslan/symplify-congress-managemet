using AutoMapper;
using Core.Application.Responses;
using Core.Persistence.Paging;
using Symplify.BackOffice.Application.Features.Languages.Commands.Create;
using Symplify.BackOffice.Application.Features.Languages.Commands.Delete;
using Symplify.BackOffice.Application.Features.Languages.Commands.Update;
using Symplify.BackOffice.Application.Features.Languages.Queries.GetById;
using Symplify.BackOffice.Application.Features.Languages.Queries.GetList;
using Symplify.BackOffice.Domain.Localization;
namespace Symplify.BackOffice.Application.Features.Languages.Profiles;
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<Language, CreateLanguageCommand>().ReverseMap();
        CreateMap<Language, CreatedLanguageResponse>().ReverseMap();
        CreateMap<Language, UpdateLanguageCommand>().ReverseMap();
        CreateMap<Language, UpdatedLanguageResponse>().ReverseMap();
        CreateMap<Language, DeletedLanguageResponse>().ReverseMap();
        CreateMap<Language, GetByIdLanguageResponse>().ReverseMap();
        CreateMap<Language, GetListLanguageListItemDto>().ReverseMap();
        CreateMap<IPaginate<Language>, GetListResponse<GetListLanguageListItemDto>>().ReverseMap();
    }
}

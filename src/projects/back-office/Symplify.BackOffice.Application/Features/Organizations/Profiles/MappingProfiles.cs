using AutoMapper;
using Core.Application.Responses;
using Core.Persistence.Paging;
using Symplify.BackOffice.Application.Features.Organizations.Commands.Create;
using Symplify.BackOffice.Application.Features.Organizations.Commands.Delete;
using Symplify.BackOffice.Application.Features.Organizations.Commands.Update;
using Symplify.BackOffice.Application.Features.Organizations.Queries.GetById;
using Symplify.BackOffice.Application.Features.Organizations.Queries.GetList;
using Symplify.BackOffice.Domain.Organization;

namespace Symplify.BackOffice.Application.Features.Organizations.Profiles;

public class MappingProfiles : AutoMapper.Profile
{
    public MappingProfiles()
    {
        CreateMap<Organization, CreateOrganizationCommand>().ReverseMap();
        CreateMap<Organization, CreatedOrganizationResponse>().ReverseMap();
        CreateMap<Organization, UpdateOrganizationCommand>().ReverseMap();
        CreateMap<Organization, UpdatedOrganizationResponse>().ReverseMap();
        CreateMap<Organization, DeletedOrganizationResponse>().ReverseMap();
        CreateMap<Organization, GetByIdOrganizationResponse>().ReverseMap();
        CreateMap<Organization, GetListOrganizationListItemDto>()
            .ForMember(destination => destination.ActiveApiKeyCount, options => options.MapFrom(source => source.ApiKeys.Count(apiKey => apiKey.IsActive)));
        CreateMap<IPaginate<Organization>, GetListResponse<GetListOrganizationListItemDto>>().ReverseMap();
    }
}

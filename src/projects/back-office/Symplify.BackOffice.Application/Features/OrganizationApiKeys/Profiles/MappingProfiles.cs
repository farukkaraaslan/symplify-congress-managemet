using AutoMapper;
using Core.Application.Responses;
using Core.Persistence.Paging;
using Symplify.BackOffice.Application.Features.OrganizationApiKeys.Commands.Create;
using Symplify.BackOffice.Application.Features.OrganizationApiKeys.Commands.Delete;
using Symplify.BackOffice.Application.Features.OrganizationApiKeys.Commands.Update;
using Symplify.BackOffice.Application.Features.OrganizationApiKeys.Queries.GetById;
using Symplify.BackOffice.Application.Features.OrganizationApiKeys.Queries.GetList;
using Symplify.BackOffice.Domain.Organization;

namespace Symplify.BackOffice.Application.Features.OrganizationApiKeys.Profiles;

public class MappingProfiles : AutoMapper.Profile
{
    public MappingProfiles()
    {
        CreateMap<OrganizationApiKey, CreatedOrganizationApiKeyResponse>().ReverseMap();
        CreateMap<OrganizationApiKey, UpdatedOrganizationApiKeyResponse>().ReverseMap();
        CreateMap<OrganizationApiKey, DeletedOrganizationApiKeyResponse>().ReverseMap();
        CreateMap<OrganizationApiKey, GetByIdOrganizationApiKeyResponse>().ReverseMap();
        CreateMap<OrganizationApiKey, GetListOrganizationApiKeyListItemDto>().ReverseMap();
        CreateMap<IPaginate<OrganizationApiKey>, GetListResponse<GetListOrganizationApiKeyListItemDto>>().ReverseMap();
    }
}

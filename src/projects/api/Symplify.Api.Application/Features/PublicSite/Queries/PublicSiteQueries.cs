using MediatR;
using Symplify.Api.Application.Features.PublicSite.Responses;
using Symplify.Api.Application.Services.PublicSite;

namespace Symplify.Api.Application.Features.PublicSite.Queries;

public sealed record GetPublicSiteBootstrapQuery(Guid OrganizationId, string? Culture)
    : IRequest<PublicSiteBootstrapResponse>;

public sealed record GetPublicSiteHomeQuery(Guid OrganizationId, string? Culture)
    : IRequest<PublicHomeResponse>;

public sealed record GetPublicSiteBoardsQuery(Guid OrganizationId, string? Culture)
    : IRequest<PublicBoardsResponse>;

public sealed record GetPublicSiteSectionsQuery(Guid OrganizationId, string? Culture)
    : IRequest<PublicSectionsResponse>;

public sealed record GetPublicSiteSectionByBindingKeyQuery(Guid OrganizationId, string BindingKey, string? Culture)
    : IRequest<PublicSectionResponse?>;

public sealed record GetPublicSiteDocumentsQuery(Guid OrganizationId, string? Culture)
    : IRequest<PublicDocumentsResponse>;

public sealed record GetPublicSiteContactQuery(Guid OrganizationId, string? Culture)
    : IRequest<PublicContactResponse>;

public sealed record GetPublicSiteContentsQuery(Guid OrganizationId, string? Culture)
    : IRequest<PublicContentsResponse>;

public sealed record GetPublicSiteLocalizationResourcesQuery(string? Culture)
    : IRequest<PublicLocalizationResourcesResponse>;

public sealed class GetPublicSiteBootstrapQueryHandler
    : IRequestHandler<GetPublicSiteBootstrapQuery, PublicSiteBootstrapResponse>
{
    private readonly IPublicSiteReadRepository _repository;

    public GetPublicSiteBootstrapQueryHandler(IPublicSiteReadRepository repository)
    {
        _repository = repository;
    }

    public Task<PublicSiteBootstrapResponse> Handle(GetPublicSiteBootstrapQuery request, CancellationToken cancellationToken)
        => _repository.GetBootstrapAsync(request.OrganizationId, request.Culture, cancellationToken);
}

public sealed class GetPublicSiteHomeQueryHandler : IRequestHandler<GetPublicSiteHomeQuery, PublicHomeResponse>
{
    private readonly IPublicSiteReadRepository _repository;

    public GetPublicSiteHomeQueryHandler(IPublicSiteReadRepository repository)
    {
        _repository = repository;
    }

    public Task<PublicHomeResponse> Handle(GetPublicSiteHomeQuery request, CancellationToken cancellationToken)
        => _repository.GetHomeAsync(request.OrganizationId, request.Culture, cancellationToken);
}

public sealed class GetPublicSiteBoardsQueryHandler : IRequestHandler<GetPublicSiteBoardsQuery, PublicBoardsResponse>
{
    private readonly IPublicSiteReadRepository _repository;

    public GetPublicSiteBoardsQueryHandler(IPublicSiteReadRepository repository)
    {
        _repository = repository;
    }

    public Task<PublicBoardsResponse> Handle(GetPublicSiteBoardsQuery request, CancellationToken cancellationToken)
        => _repository.GetBoardsAsync(request.OrganizationId, request.Culture, cancellationToken);
}

public sealed class GetPublicSiteSectionsQueryHandler : IRequestHandler<GetPublicSiteSectionsQuery, PublicSectionsResponse>
{
    private readonly IPublicSiteReadRepository _repository;

    public GetPublicSiteSectionsQueryHandler(IPublicSiteReadRepository repository)
    {
        _repository = repository;
    }

    public Task<PublicSectionsResponse> Handle(GetPublicSiteSectionsQuery request, CancellationToken cancellationToken)
        => _repository.GetSectionsAsync(request.OrganizationId, request.Culture, cancellationToken);
}

public sealed class GetPublicSiteSectionByBindingKeyQueryHandler
    : IRequestHandler<GetPublicSiteSectionByBindingKeyQuery, PublicSectionResponse?>
{
    private readonly IPublicSiteReadRepository _repository;

    public GetPublicSiteSectionByBindingKeyQueryHandler(IPublicSiteReadRepository repository)
    {
        _repository = repository;
    }

    public Task<PublicSectionResponse?> Handle(GetPublicSiteSectionByBindingKeyQuery request, CancellationToken cancellationToken)
        => _repository.GetSectionByBindingKeyAsync(request.OrganizationId, request.BindingKey, request.Culture, cancellationToken);
}

public sealed class GetPublicSiteDocumentsQueryHandler : IRequestHandler<GetPublicSiteDocumentsQuery, PublicDocumentsResponse>
{
    private readonly IPublicSiteReadRepository _repository;

    public GetPublicSiteDocumentsQueryHandler(IPublicSiteReadRepository repository)
    {
        _repository = repository;
    }

    public Task<PublicDocumentsResponse> Handle(GetPublicSiteDocumentsQuery request, CancellationToken cancellationToken)
        => _repository.GetDocumentsAsync(request.OrganizationId, request.Culture, cancellationToken);
}

public sealed class GetPublicSiteContactQueryHandler : IRequestHandler<GetPublicSiteContactQuery, PublicContactResponse>
{
    private readonly IPublicSiteReadRepository _repository;

    public GetPublicSiteContactQueryHandler(IPublicSiteReadRepository repository)
    {
        _repository = repository;
    }

    public Task<PublicContactResponse> Handle(GetPublicSiteContactQuery request, CancellationToken cancellationToken)
        => _repository.GetContactAsync(request.OrganizationId, request.Culture, cancellationToken);
}

public sealed class GetPublicSiteContentsQueryHandler : IRequestHandler<GetPublicSiteContentsQuery, PublicContentsResponse>
{
    private readonly IPublicSiteReadRepository _repository;

    public GetPublicSiteContentsQueryHandler(IPublicSiteReadRepository repository)
    {
        _repository = repository;
    }

    public Task<PublicContentsResponse> Handle(GetPublicSiteContentsQuery request, CancellationToken cancellationToken)
        => _repository.GetContentsAsync(request.OrganizationId, request.Culture, cancellationToken);
}

public sealed class GetPublicSiteLocalizationResourcesQueryHandler
    : IRequestHandler<GetPublicSiteLocalizationResourcesQuery, PublicLocalizationResourcesResponse>
{
    private readonly IPublicSiteReadRepository _repository;

    public GetPublicSiteLocalizationResourcesQueryHandler(IPublicSiteReadRepository repository)
    {
        _repository = repository;
    }

    public async Task<PublicLocalizationResourcesResponse> Handle(GetPublicSiteLocalizationResourcesQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, string> resources = await _repository.GetLocalizationResourcesAsync(request.Culture, cancellationToken);

        return new PublicLocalizationResourcesResponse
        {
            Culture = request.Culture ?? string.Empty,
            Resources = resources
        };
    }
}

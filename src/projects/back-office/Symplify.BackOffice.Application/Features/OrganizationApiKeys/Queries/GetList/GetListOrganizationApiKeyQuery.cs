using System.Linq.Expressions;
using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using Core.Persistence.Paging;
using MediatR;
using Symplify.BackOffice.Application.Features.OrganizationApiKeys.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Organization;

namespace Symplify.BackOffice.Application.Features.OrganizationApiKeys.Queries.GetList;

public class GetListOrganizationApiKeyQuery : IRequest<GetListResponse<GetListOrganizationApiKeyListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();

    public Guid? OrganizationId { get; set; }

    public string? SearchText { get; set; }

    public string SortColumn { get; set; } = "createdDate";

    public string SortDirection { get; set; } = "desc";

    public string[] Roles => new[]
    {
        OrganizationApiKeysOperationClaims.Admin,
        OrganizationApiKeysOperationClaims.Read
    };

    public bool BypassCache { get; }

    public string CacheKey => $"GetListOrganizationApiKeys({PageRequest.Page},{PageRequest.PageSize},{OrganizationId},{SearchText},{SortColumn},{SortDirection})";

    public string CacheGroupKey => "GetOrganizationApiKeys";

    public TimeSpan? SlidingExpiration { get; }

    public class GetListOrganizationApiKeyQueryHandler : IRequestHandler<GetListOrganizationApiKeyQuery, GetListResponse<GetListOrganizationApiKeyListItemDto>>
    {
        private readonly IOrganizationApiKeyRepository _repository;
        private readonly IMapper _mapper;

        public GetListOrganizationApiKeyQueryHandler(IOrganizationApiKeyRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<GetListResponse<GetListOrganizationApiKeyListItemDto>> Handle(GetListOrganizationApiKeyQuery request, CancellationToken cancellationToken)
        {
            IPaginate<OrganizationApiKey> entities = await _repository.GetListAsync(
                predicate: BuildPredicate(request),
                orderBy: BuildOrderBy(request.SortColumn, request.SortDirection),
                index: request.PageRequest.Page < 0 ? 0 : request.PageRequest.Page,
                size: request.PageRequest.PageSize <= 0 ? 20 : request.PageRequest.PageSize,
                cancellationToken: cancellationToken);

            return _mapper.Map<GetListResponse<GetListOrganizationApiKeyListItemDto>>(entities);
        }

        private static Expression<Func<OrganizationApiKey, bool>>? BuildPredicate(GetListOrganizationApiKeyQuery request)
        {
            bool hasOrganizationFilter = request.OrganizationId.HasValue && request.OrganizationId.Value != Guid.Empty;
            Guid organizationId = request.OrganizationId.GetValueOrDefault();
            string? searchText = NormalizeSearchText(request.SearchText);

            if (string.IsNullOrWhiteSpace(searchText))
            {
                return hasOrganizationFilter
                    ? entity => entity.OrganizationId == organizationId
                    : null;
            }

            string normalizedSearchText = searchText.ToLower();

            if (hasOrganizationFilter)
            {
                return entity =>
                    entity.OrganizationId == organizationId &&
                    ((entity.Name != null && entity.Name.ToLower().Contains(normalizedSearchText)) ||
                     (entity.Environment != null && entity.Environment.ToLower().Contains(normalizedSearchText)) ||
                     (entity.KeyType != null && entity.KeyType.ToLower().Contains(normalizedSearchText)) ||
                     (entity.KeyPrefix != null && entity.KeyPrefix.ToLower().Contains(normalizedSearchText)) ||
                     (entity.Description != null && entity.Description.ToLower().Contains(normalizedSearchText)) ||
                     (entity.Scopes != null && entity.Scopes.ToLower().Contains(normalizedSearchText)));
            }

            return entity =>
                (entity.Name != null && entity.Name.ToLower().Contains(normalizedSearchText)) ||
                (entity.Environment != null && entity.Environment.ToLower().Contains(normalizedSearchText)) ||
                (entity.KeyType != null && entity.KeyType.ToLower().Contains(normalizedSearchText)) ||
                (entity.KeyPrefix != null && entity.KeyPrefix.ToLower().Contains(normalizedSearchText)) ||
                (entity.Description != null && entity.Description.ToLower().Contains(normalizedSearchText)) ||
                (entity.Scopes != null && entity.Scopes.ToLower().Contains(normalizedSearchText));
        }

        private static Func<IQueryable<OrganizationApiKey>, IOrderedQueryable<OrganizationApiKey>> BuildOrderBy(string? sortColumn, string? sortDirection)
        {
            bool descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            string normalizedSortColumn = string.IsNullOrWhiteSpace(sortColumn)
                ? "createddate"
                : sortColumn.Trim().ToLowerInvariant();

            return normalizedSortColumn switch
            {
                "name" => query => descending
                    ? query.OrderByDescending(entity => entity.Name).ThenByDescending(entity => entity.CreatedDate)
                    : query.OrderBy(entity => entity.Name).ThenByDescending(entity => entity.CreatedDate),

                "environment" => query => descending
                    ? query.OrderByDescending(entity => entity.Environment).ThenByDescending(entity => entity.CreatedDate)
                    : query.OrderBy(entity => entity.Environment).ThenByDescending(entity => entity.CreatedDate),

                "keytype" => query => descending
                    ? query.OrderByDescending(entity => entity.KeyType).ThenByDescending(entity => entity.CreatedDate)
                    : query.OrderBy(entity => entity.KeyType).ThenByDescending(entity => entity.CreatedDate),

                "isactive" => query => descending
                    ? query.OrderByDescending(entity => entity.IsActive).ThenByDescending(entity => entity.CreatedDate)
                    : query.OrderBy(entity => entity.IsActive).ThenByDescending(entity => entity.CreatedDate),

                "lastusedat" => query => descending
                    ? query.OrderByDescending(entity => entity.LastUsedAt).ThenByDescending(entity => entity.CreatedDate)
                    : query.OrderBy(entity => entity.LastUsedAt).ThenByDescending(entity => entity.CreatedDate),

                _ => query => descending
                    ? query.OrderByDescending(entity => entity.CreatedDate).ThenByDescending(entity => entity.Id)
                    : query.OrderBy(entity => entity.CreatedDate).ThenBy(entity => entity.Id)
            };
        }

        private static string? NormalizeSearchText(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}

using System.Linq.Expressions;
using Core.Application.Pipelines.Authorization;
using Core.Application.Requests;
using Core.Application.Responses;
using Core.Application.Storage;
using Core.Persistence.Paging;
using MediatR;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Common.Storage;
using Symplify.BackOffice.Application.Features.Organizations.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Organization;

namespace Symplify.BackOffice.Application.Features.Organizations.Queries.GetList;

public class GetListOrganizationQuery : IRequest<GetListResponse<GetListOrganizationListItemDto>>, ISecuredRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public string? SearchText { get; set; }
    public string? SortColumn { get; set; }
    public string? SortDirection { get; set; }

    public string[] Roles => new[] { OrganizationsOperationClaims.Admin, OrganizationsOperationClaims.Read };

    public class GetListOrganizationQueryHandler : IRequestHandler<GetListOrganizationQuery, GetListResponse<GetListOrganizationListItemDto>>
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IOrganizationApiKeyRepository _apiKeyRepository;
        private readonly IObjectStorageService _objectStorageService;
        private readonly ObjectStorageOptions _storageOptions;

        public GetListOrganizationQueryHandler(
            IOrganizationRepository organizationRepository,
            IOrganizationApiKeyRepository apiKeyRepository,
            IObjectStorageService objectStorageService,
            IOptions<ObjectStorageOptions> storageOptions)
        {
            _organizationRepository = organizationRepository;
            _apiKeyRepository = apiKeyRepository;
            _objectStorageService = objectStorageService;
            _storageOptions = storageOptions.Value;
        }

        public async Task<GetListResponse<GetListOrganizationListItemDto>> Handle(
            GetListOrganizationQuery request,
            CancellationToken cancellationToken)
        {
            Expression<Func<Organization, bool>>? predicate = BuildPredicate(request.SearchText);

            IPaginate<Organization> organizations = await _organizationRepository.GetListAsync(
                predicate: predicate,
                orderBy: BuildOrderBy(request.SortColumn, request.SortDirection),
                index: request.PageRequest.Page,
                size: request.PageRequest.PageSize,
                cancellationToken: cancellationToken);

            HashSet<Guid> organizationIds = organizations.Items
                .Select(organization => organization.Id)
                .ToHashSet();

            DateTime utcNow = DateTime.UtcNow;

            Dictionary<Guid, int> activeApiKeyCounts = organizationIds.Count == 0
                ? new Dictionary<Guid, int>()
                : _apiKeyRepository.Query()
                    .Where(apiKey => organizationIds.Contains(apiKey.OrganizationId)
                        && apiKey.IsActive
                        && apiKey.RevokedAt == null
                        && (apiKey.ExpiresAt == null || apiKey.ExpiresAt > utcNow))
                    .GroupBy(apiKey => apiKey.OrganizationId)
                    .ToDictionary(group => group.Key, group => group.Count());

            List<GetListOrganizationListItemDto> items = new();

            foreach (Organization organization in organizations.Items)
            {
                items.Add(new GetListOrganizationListItemDto
                {
                    Id = organization.Id,
                    Name = organization.Name,
                    Code = organization.Code,
                    Slug = organization.Slug,
                    ShortName = organization.ShortName,
                    WebsiteUrl = organization.WebsiteUrl,
                    HostUrl = organization.HostUrl,
                    LogoLightPath = organization.LogoLightPath,
                    LogoDarkPath = organization.LogoDarkPath,
                    LogoLightUrl = await ResolveImageUrlAsync(organization.LogoLightPath, cancellationToken),
                    LogoDarkUrl = await ResolveImageUrlAsync(organization.LogoDarkPath, cancellationToken),
                    BrandColor = organization.BrandColor,
                    IsActive = organization.IsActive,
                    ActiveApiKeyCount = activeApiKeyCounts.TryGetValue(organization.Id, out int count) ? count : 0,
                    CreatedDate = organization.CreatedDate,
                    UpdatedDate = organization.UpdatedDate
                });
            }

            return new GetListResponse<GetListOrganizationListItemDto>
            {
                Index = organizations.Index,
                Size = organizations.Size,
                Count = organizations.Count,
                Pages = organizations.Pages,
                HasPrevious = organizations.HasPrevious,
                HasNext = organizations.HasNext,
                Items = items
            };
        }

        private async Task<string?> ResolveImageUrlAsync(string? objectName, CancellationToken cancellationToken)
        {
            return await BackOfficeObjectStorageHelper.GetReadUrlOrPathAsync(
                _objectStorageService,
                GetCongressImagesBucketNameOrNull(),
                objectName,
                TimeSpan.FromMinutes(10),
                cancellationToken);
        }

        private string? GetCongressImagesBucketNameOrNull()
        {
            return string.IsNullOrWhiteSpace(_storageOptions.Buckets.CongressImages)
                ? null
                : _storageOptions.Buckets.CongressImages.Trim();
        }

        private static Expression<Func<Organization, bool>>? BuildPredicate(string? searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return null;

            string keyword = searchText.Trim().ToLowerInvariant();

            return organization =>
                organization.Name.ToLower().Contains(keyword) ||
                organization.Code.ToLower().Contains(keyword) ||
                organization.Slug.ToLower().Contains(keyword) ||
                (organization.ShortName != null && organization.ShortName.ToLower().Contains(keyword)) ||
                (organization.ContactEmail != null && organization.ContactEmail.ToLower().Contains(keyword));
        }

        private static Func<IQueryable<Organization>, IOrderedQueryable<Organization>> BuildOrderBy(
            string? sortColumn,
            string? sortDirection)
        {
            bool descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            string column = sortColumn?.Trim().ToLowerInvariant() ?? "name";

            return column switch
            {
                "code" => query => descending ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code),
                "brandcolor" => query => descending ? query.OrderByDescending(x => x.BrandColor) : query.OrderBy(x => x.BrandColor),
                "isactive" => query => descending ? query.OrderByDescending(x => x.IsActive) : query.OrderBy(x => x.IsActive),
                "updateddate" => query => descending ? query.OrderByDescending(x => x.UpdatedDate) : query.OrderBy(x => x.UpdatedDate),
                _ => query => descending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            };
        }
    }
}

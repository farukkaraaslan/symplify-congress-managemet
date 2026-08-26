using System.Security.Claims;
using Core.Application.Responses;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Symplify.BackOffice.Application.Common.Authorization;
using Symplify.BackOffice.Application.Features.Roles.Dtos;
using Symplify.BackOffice.Application.Services.RoleAdministration;
using Symplify.BackOffice.Domain.Identity;
using Symplify.BackOffice.Infrastructure.Identity.Constants;

namespace Symplify.BackOffice.Infrastructure.Identity;

public sealed class BackOfficeRoleAdministrationService : IRoleAdministrationService
{
    private const string PermissionClaimType = "Permission";
    private const string SuperAdminRoleName = "SuperAdmin";

    private readonly RoleManager<AppRole> _roleManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly IMemoryCache _memoryCache;

    public BackOfficeRoleAdministrationService(
        RoleManager<AppRole> roleManager,
        UserManager<AppUser> userManager,
        IMemoryCache memoryCache)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _memoryCache = memoryCache;
    }

    public async Task<GetListResponse<RoleListItemDto>> GetListAsync(
        int page,
        int pageSize,
        string? searchText,
        CancellationToken cancellationToken = default)
    {
        page = page < 0 ? 0 : page;
        pageSize = pageSize <= 0 ? 20 : pageSize;

        IQueryable<AppRole> query = _roleManager.Roles
            .AsNoTracking()
            .Where(role => role.DeletedDate == null);

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            string normalizedSearch = searchText.Trim().ToLower();

            query = query.Where(role =>
                (role.Name != null && role.Name.ToLower().Contains(normalizedSearch)) ||
                (role.Description != null && role.Description.ToLower().Contains(normalizedSearch)));
        }

        int count = await query.CountAsync(cancellationToken);

        List<AppRole> roles = await query
            .OrderBy(role => role.Name)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        List<RoleListItemDto> items = new();

        foreach (AppRole role in roles)
        {
            IList<Claim> claims = await _roleManager.GetClaimsAsync(role);
            int userCount = await GetActiveUserCountInRoleAsync(role.Name);

            items.Add(new RoleListItemDto
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                Description = role.Description,
                UserCount = userCount,
                ClaimCount = claims.Count(claim => claim.Type == PermissionClaimType),
                CreatedDate = role.CreatedDate
            });
        }

        int pages = count == 0 ? 0 : (int)Math.Ceiling(count / (double)pageSize);

        return new GetListResponse<RoleListItemDto>
        {
            Index = page,
            Size = pageSize,
            Count = count,
            Pages = pages,
            HasPrevious = page > 0,
            HasNext = page + 1 < pages,
            Items = items
        };
    }

    public async Task<RoleDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        AppRole role = await GetRoleOrThrowAsync(id);
        return await MapDetailAsync(role);
    }

    public async Task<RoleDetailDto> CreateAsync(CreateRoleRequestDto request, CancellationToken cancellationToken = default)
    {
        string roleName = NormalizeRoleName(request.Name);

        AppRole? existing = await _roleManager.FindByNameAsync(roleName);
        if (existing is not null && existing.DeletedDate is null)
            throw new BusinessException(RoleMessages.RoleNameAlreadyExists);

        AppRole role = existing ?? new AppRole(roleName)
        {
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "System"
        };

        role.Name = roleName;
        role.NormalizedName = _roleManager.NormalizeKey(roleName);
        role.Description = NormalizeOptional(request.Description);
        role.DeletedDate = null;
        role.DeletedBy = null;

        IdentityResult result = existing is null
            ? await _roleManager.CreateAsync(role)
            : await _roleManager.UpdateAsync(role);

        ThrowIfFailed(result, RoleMessages.RoleCreateFailed);

        await UpdateClaimsAsync(role.Id, request.ClaimNames, cancellationToken);
        return await MapDetailAsync(role);
    }

    public async Task<RoleDetailDto> UpdateAsync(UpdateRoleRequestDto request, CancellationToken cancellationToken = default)
    {
        AppRole role = await GetRoleOrThrowAsync(request.Id);
        string previousRoleName = role.Name ?? string.Empty;
        string roleName = NormalizeRoleName(request.Name);

        AppRole? existingWithName = await _roleManager.FindByNameAsync(roleName);
        if (existingWithName is not null && existingWithName.Id != role.Id && existingWithName.DeletedDate is null)
            throw new BusinessException(RoleMessages.RoleNameTakenByAnother);

        role.Name = roleName;
        role.NormalizedName = _roleManager.NormalizeKey(roleName);
        role.Description = NormalizeOptional(request.Description);
        role.UpdatedDate = DateTime.UtcNow;
        role.UpdatedBy = "System";

        IdentityResult result = await _roleManager.UpdateAsync(role);
        ThrowIfFailed(result, RoleMessages.RoleUpdateFailed);

        if (!string.Equals(previousRoleName, role.Name, StringComparison.OrdinalIgnoreCase))
            _memoryCache.Remove($"BackOffice:RoleClaims:{previousRoleName}");

        await ClearRoleClaimsCacheAsync(role.Name);

        return await MapDetailAsync(role);
    }

    public async Task UpdateClaimsAsync(Guid roleId, IReadOnlyCollection<string> claimNames, CancellationToken cancellationToken = default)
    {
        AppRole role = await GetRoleOrThrowAsync(roleId);

        HashSet<string> catalog = OperationClaimCatalog.GetAll().ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> requestedClaims = claimNames
            .Where(claim => !string.IsNullOrWhiteSpace(claim))
            .Select(claim => claim.Trim())
            .Where(claim => catalog.Contains(claim))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        IList<Claim> currentClaims = await _roleManager.GetClaimsAsync(role);
        IEnumerable<Claim> currentPermissionClaims = currentClaims
            .Where(claim => claim.Type == PermissionClaimType)
            .ToList();

        foreach (Claim claim in currentPermissionClaims)
            ThrowIfFailed(await _roleManager.RemoveClaimAsync(role, claim), RoleMessages.RolePermissionsUpdateFailed);

        IEnumerable<Claim> newClaims = requestedClaims
            .OrderBy(claim => claim)
            .Select(claim => new Claim(PermissionClaimType, claim));

        foreach (Claim claim in newClaims)
            ThrowIfFailed(await _roleManager.AddClaimAsync(role, claim), RoleMessages.RolePermissionsUpdateFailed);

        role.UpdatedDate = DateTime.UtcNow;
        role.UpdatedBy = "System";
        ThrowIfFailed(await _roleManager.UpdateAsync(role), RoleMessages.RolePermissionsUpdateFailed);

        await ClearRoleClaimsCacheAsync(role.Name);
    }

    public async Task DeleteAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        AppRole role = await GetRoleOrThrowAsync(roleId);

        if (string.Equals(role.Name, SuperAdminRoleName, StringComparison.OrdinalIgnoreCase))
            throw new BusinessException(RoleMessages.SuperAdminRoleCannotBeDeleted);

        int assignedUserCount = await GetActiveUserCountInRoleAsync(role.Name);

        if (assignedUserCount > 0)
            throw new BusinessException(RoleMessages.RoleAssignedToUserCannotBeDeleted);

        role.DeletedDate = DateTime.UtcNow;
        role.DeletedBy = "System";
        role.UpdatedDate = DateTime.UtcNow;
        role.UpdatedBy = "System";

        IdentityResult result = await _roleManager.UpdateAsync(role);
        ThrowIfFailed(result, RoleMessages.RoleDeleteFailed);

        await ClearRoleClaimsCacheAsync(role.Name);
    }

    private async Task<RoleDetailDto> MapDetailAsync(AppRole role)
    {
        IList<Claim> claims = await _roleManager.GetClaimsAsync(role);

        IReadOnlyList<string> assignedClaims = claims
            .Where(claim => claim.Type == PermissionClaimType)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(claim => claim)
            .ToList();

        HashSet<string> assignedClaimSet = assignedClaims.ToHashSet(StringComparer.OrdinalIgnoreCase);

        IReadOnlyList<RoleClaimOptionDto> availableClaims = OperationClaimCatalog.GetAll()
            .Select(claim => new RoleClaimOptionDto
            {
                Name = claim,
                Module = ResolveModule(claim),
                IsAssigned = assignedClaimSet.Contains(claim)
            })
            .ToList();

        return new RoleDetailDto
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty,
            Description = role.Description,
            CreatedDate = role.CreatedDate,
            UpdatedDate = role.UpdatedDate,
            AssignedClaims = assignedClaims,
            AvailableClaims = availableClaims
        };
    }

    private async Task<AppRole> GetRoleOrThrowAsync(Guid roleId)
    {
        AppRole? role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role is null || role.DeletedDate is not null)
            throw new BusinessException(RoleMessages.RoleNotFound);

        return role;
    }

    private async Task<int> GetActiveUserCountInRoleAsync(string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return 0;

        IList<AppUser> usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
        return usersInRole.Count(user => user.DeletedDate is null);
    }

    private async Task ClearRoleClaimsCacheAsync(string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return;

        _memoryCache.Remove($"BackOffice:RoleClaims:{roleName}");

        IList<AppUser> usersInRole = await _userManager.GetUsersInRoleAsync(roleName);

        foreach (AppUser user in usersInRole)
            _memoryCache.Remove($"BackOffice:UserOperationClaims:{user.Id}");
    }

    private static string NormalizeRoleName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessException(RoleMessages.RoleNameRequired);

        return name.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string ResolveModule(string permission)
    {
        int separatorIndex = permission.IndexOf('.', StringComparison.Ordinal);
        return separatorIndex > 0 ? permission[..separatorIndex] : permission;
    }

    private static void ThrowIfFailed(IdentityResult result, string message)
    {
        if (result.Succeeded)
            return;

        string details = string.Join(" ", result.Errors.Select(error => error.Description));
        throw new BusinessException(string.IsNullOrWhiteSpace(details) ? message : $"{message} {details}");
    }
}

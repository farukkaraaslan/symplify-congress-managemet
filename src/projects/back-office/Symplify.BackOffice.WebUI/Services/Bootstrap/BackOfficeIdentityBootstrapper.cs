using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Symplify.BackOffice.Application.Common.Authorization;
using Symplify.BackOffice.Domain.Identity;

namespace Symplify.BackOffice.WebUI.Services.Bootstrap;

public sealed class BackOfficeIdentityBootstrapper : IBackOfficeIdentityBootstrapper
{
    private const string PermissionClaimType = "Permission";
    private const string SuperAdminRoleName = "SuperAdmin";

    private const string DefaultAdminEmail = "admin@symplify.com";
    private const string DefaultAdminPassword = "Admin1234!";

    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly ILogger<BackOfficeIdentityBootstrapper> _logger;

    public BackOfficeIdentityBootstrapper(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        ILogger<BackOfficeIdentityBootstrapper> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task BootstrapAsync(CancellationToken cancellationToken = default)
    {
        AppRole superAdminRole = await EnsureSuperAdminRoleAsync();

        await EnsureSuperAdminRoleClaimsAsync(superAdminRole);

        AppUser superAdminUser = await EnsureSuperAdminUserAsync();

        await EnsureUserRoleAsync(superAdminUser, SuperAdminRoleName);
    }

    private async Task<AppRole> EnsureSuperAdminRoleAsync()
    {
        AppRole? role = await _roleManager.FindByNameAsync(SuperAdminRoleName);

        if (role is not null)
            return role;

        role = new AppRole(SuperAdminRoleName)
        {
            Description = "System owner role with all permissions.",
            CreatedDate = DateTime.UtcNow
        };

        IdentityResult result = await _roleManager.CreateAsync(role);

        ThrowIfFailed(result, "SuperAdmin role could not be created.");

        _logger.LogInformation("SuperAdmin role created.");

        return role;
    }

    private async Task EnsureSuperAdminRoleClaimsAsync(AppRole role)
    {
        IList<Claim> existingClaims = await _roleManager.GetClaimsAsync(role);

        HashSet<string> existingPermissionValues = existingClaims
            .Where(claim => claim.Type == PermissionClaimType)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        IReadOnlyList<string> permissions = OperationClaimCatalog.GetAll();

        foreach (string permission in permissions)
        {
            if (existingPermissionValues.Contains(permission))
                continue;

            IdentityResult result = await _roleManager.AddClaimAsync(
                role,
                new Claim(PermissionClaimType, permission));

            ThrowIfFailed(result, $"Permission claim could not be added to SuperAdmin role. Permission: {permission}");
        }

        _logger.LogInformation(
            "SuperAdmin permissions synchronized. Permission count: {PermissionCount}",
            permissions.Count);
    }

    private async Task<AppUser> EnsureSuperAdminUserAsync()
    {
        AppUser? user = await _userManager.FindByEmailAsync(DefaultAdminEmail);

        if (user is not null)
            return user;

        user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = DefaultAdminEmail,
            NormalizedUserName = DefaultAdminEmail.ToUpperInvariant(),
            Email = DefaultAdminEmail,
            NormalizedEmail = DefaultAdminEmail.ToUpperInvariant(),
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            CreatedDate = DateTime.UtcNow
        };

        TrySetOptionalUserProperty(user, "Name", "Super");
        TrySetOptionalUserProperty(user, "Surname", "Admin");
        TrySetOptionalUserProperty(user, "FirstName", "Super");
        TrySetOptionalUserProperty(user, "LastName", "Admin");

        IdentityResult result = await _userManager.CreateAsync(user, DefaultAdminPassword);

        ThrowIfFailed(result, "Default SuperAdmin user could not be created.");

        _logger.LogInformation("Default SuperAdmin user created. Email: {Email}", DefaultAdminEmail);

        return user;
    }

    private async Task EnsureUserRoleAsync(AppUser user, string roleName)
    {
        bool isInRole = await _userManager.IsInRoleAsync(user, roleName);

        if (isInRole)
            return;

        IdentityResult result = await _userManager.AddToRoleAsync(user, roleName);

        ThrowIfFailed(result, $"Default user could not be assigned to role {roleName}.");

        _logger.LogInformation(
            "Default user assigned to role. Email: {Email}, Role: {Role}",
            user.Email,
            roleName);
    }

    private static void ThrowIfFailed(IdentityResult result, string message)
    {
        if (result.Succeeded)
            return;

        string errors = string.Join(
            " | ",
            result.Errors.Select(error => $"{error.Code}: {error.Description}"));

        throw new InvalidOperationException($"{message} {errors}");
    }

    private static void TrySetOptionalUserProperty(AppUser user, string propertyName, object? value)
    {
        var property = typeof(AppUser).GetProperty(propertyName);

        if (property is null || !property.CanWrite)
            return;

        property.SetValue(user, value);
    }
}

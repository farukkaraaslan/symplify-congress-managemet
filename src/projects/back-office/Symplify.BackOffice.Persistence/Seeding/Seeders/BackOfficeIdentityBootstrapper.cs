using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Symplify.BackOffice.Application.Common.Authorization;
using Symplify.BackOffice.Domain.Identity;
using Symplify.BackOffice.Persistence.Contexts;
using Symplify.BackOffice.Persistence.Seeding.Abstractions;

namespace Symplify.BackOffice.Persistence.Seeding.Seeders;

public sealed class BackOfficeIdentityBootstrapper : IBackOfficeIdentityBootstrapper
{
    private const string PermissionClaimType = "Permission";
    private const string SuperAdminRoleName = "SuperAdmin";

    private const string DefaultAdminEmail = "admin@symplify.com";
    private const string DefaultAdminPassword = "Admin1234!";

    private readonly BackOfficeDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly ILogger<BackOfficeIdentityBootstrapper> _logger;

    public BackOfficeIdentityBootstrapper(
        BackOfficeDbContext context,
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        ILogger<BackOfficeIdentityBootstrapper> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task BootstrapAsync(CancellationToken cancellationToken = default)
    {
        AppRole superAdminRole = await EnsureSuperAdminRoleAsync();

        await EnsureSuperAdminRoleClaimsAsync(superAdminRole, cancellationToken);

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
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "System"
        };

        IdentityResult result = await _roleManager.CreateAsync(role);

        ThrowIfFailed(result, "SuperAdmin role could not be created.");

        _logger.LogInformation("SuperAdmin role created.");

        return role;
    }

    private async Task EnsureSuperAdminRoleClaimsAsync(
        AppRole role,
        CancellationToken cancellationToken)
    {
        List<AppRoleClaim> existingClaims = await _context.Set<AppRoleClaim>()
            .Where(roleClaim =>
                roleClaim.RoleId == role.Id &&
                roleClaim.ClaimType == PermissionClaimType)
            .ToListAsync(cancellationToken);

        IReadOnlyList<string> permissions = OperationClaimCatalog.GetAll();
        DateTime utcNow = DateTime.UtcNow;

        foreach (string permission in permissions)
        {
            AppRoleClaim? existingClaim = existingClaims.FirstOrDefault(roleClaim =>
                string.Equals(roleClaim.ClaimValue, permission, StringComparison.OrdinalIgnoreCase));

            PermissionMetadata metadata = ResolvePermissionMetadata(permission);

            if (existingClaim is null)
            {
                await _context.Set<AppRoleClaim>().AddAsync(new AppRoleClaim
                {
                    RoleId = role.Id,
                    ClaimType = PermissionClaimType,
                    ClaimValue = permission,
                    Module = metadata.Module,
                    DisplayName = metadata.DisplayName,
                    Description = metadata.Description,
                    CreatedDate = utcNow,
                    CreatedBy = "System"
                }, cancellationToken);

                continue;
            }

            bool changed = false;

            if (string.IsNullOrWhiteSpace(existingClaim.Module))
            {
                existingClaim.Module = metadata.Module;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(existingClaim.DisplayName))
            {
                existingClaim.DisplayName = metadata.DisplayName;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(existingClaim.Description))
            {
                existingClaim.Description = metadata.Description;
                changed = true;
            }

            if (existingClaim.CreatedDate == default)
            {
                existingClaim.CreatedDate = utcNow;
                changed = true;
            }

            if (changed)
            {
                existingClaim.UpdatedDate = utcNow;
                existingClaim.UpdatedBy = "System";
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

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
            Name = "Super",
            Surname = "Admin",
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "System"
        };

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

    private static PermissionMetadata ResolvePermissionMetadata(string permission)
    {
        string module = permission;

        int separatorIndex = permission.IndexOf('.', StringComparison.Ordinal);

        if (separatorIndex > 0)
            module = permission[..separatorIndex];

        string action = separatorIndex > 0 && separatorIndex < permission.Length - 1
            ? permission[(separatorIndex + 1)..]
            : permission;

        return new PermissionMetadata(
            Module: module,
            DisplayName: $"{SplitPascalCase(module)} {SplitPascalCase(action)}",
            Description: $"{permission} permission.");
    }

    private static string SplitPascalCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return System.Text.RegularExpressions.Regex.Replace(
            value,
            "([a-z0-9])([A-Z])",
            "$1 $2");
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

    private sealed record PermissionMetadata(
        string Module,
        string DisplayName,
        string Description);
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Symplify.BackOffice.Application.Common.Authorization;
using Symplify.BackOffice.Application.Features.Roles.Constants;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Identity;
using Symplify.BackOffice.Domain.Organization;
using Symplify.BackOffice.Domain.Submission;
using Symplify.BackOffice.Persistence.Contexts;
using Symplify.BackOffice.Persistence.Seeding.Abstractions;
using Symplify.BackOffice.Persistence.Seeding.Definitions;

namespace Symplify.BackOffice.Persistence.Seeding.Seeders;

public sealed class BackOfficeIdentityBootstrapper : IBackOfficeIdentityBootstrapper
{
    private const string PermissionClaimType = "Permission";
    private const string SuperAdminRoleName = BackOfficeDemoSeedDefinition.SuperAdminRoleName;
    private const string DefaultAdminEmailFallback = "admin@symplify.com";
    private const string DefaultAdminPasswordFallback = BackOfficeDemoSeedDefinition.DefaultPassword;

    private readonly BackOfficeDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly ILogger<BackOfficeIdentityBootstrapper> _logger;

    public BackOfficeIdentityBootstrapper(
        BackOfficeDbContext context,
        IConfiguration configuration,
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        ILogger<BackOfficeIdentityBootstrapper> logger)
    {
        _context = context;
        _configuration = configuration;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task BootstrapAsync(CancellationToken cancellationToken = default)
    {
        await EnsureApplicationRolesAsync(cancellationToken);

        AppUser superAdminUser = await EnsureSuperAdminUserAsync();

        await EnsureUserRoleAsync(superAdminUser, SuperAdminRoleName);
    }

    private async Task EnsureApplicationRolesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<string> allPermissions = OperationClaimCatalog.GetAll();

        AppRole superAdminRole = await EnsureRoleAsync(SuperAdminRoleName, "System owner role with all permissions.");
        await EnsureRoleClaimsAsync(superAdminRole, allPermissions, cancellationToken);

        AppRole organizationAdminRole = await EnsureRoleAsync(
            BackOfficeDemoSeedDefinition.OrganizationAdminRoleName,
            "Organization level administrator role for test organizations.");
        await EnsureRoleClaimsAsync(organizationAdminRole, allPermissions, cancellationToken);

        AppRole congressEditorRole = await EnsureRoleAsync(
            BackOfficeDemoSeedDefinition.CongressEditorRoleName,
            "Congress editor role for managing submissions and congress content.");
        await EnsureRoleClaimsAsync(congressEditorRole, allPermissions, cancellationToken);

        AppRole reviewerRole = await EnsureRoleAsync(
            BackOfficeDemoSeedDefinition.ReviewerRoleName,
            "Reviewer role for evaluation workflows.");
        await EnsureRoleClaimsAsync(reviewerRole, SelectReviewerPermissions(allPermissions), cancellationToken, replaceExisting: true);

        AppRole authorRole = await EnsureRoleAsync(
            BackOfficeDemoSeedDefinition.AuthorRoleName,
            "Author role for creating and tracking own submissions.");
        await EnsureRoleClaimsAsync(authorRole, SelectAuthorPermissions(allPermissions), cancellationToken, replaceExisting: true);

        AppRole roleManagerRole = await EnsureRoleAsync(
            "RoleManager",
            "Role management operator role.");
        await EnsureRoleClaimsAsync(roleManagerRole, SelectRoleManagerPermissions(allPermissions), cancellationToken, replaceExisting: true);
    }

    private async Task<AppRole> EnsureRoleAsync(string roleName, string description)
    {
        AppRole? role = await _roleManager.FindByNameAsync(roleName);

        if (role is not null)
        {
            if (string.IsNullOrWhiteSpace(role.Description))
                role.Description = description;

            return role;
        }

        role = new AppRole(roleName)
        {
            Description = description,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "System"
        };

        IdentityResult result = await _roleManager.CreateAsync(role);

        ThrowIfFailed(result, $"Role could not be created. Role: {roleName}.");

        _logger.LogInformation("Role created. Role: {RoleName}", roleName);

        return role;
    }

    private async Task EnsureRoleClaimsAsync(
        AppRole role,
        IReadOnlyList<string> permissions,
        CancellationToken cancellationToken,
        bool replaceExisting = false)
    {
        List<AppRoleClaim> existingClaims = await _context.Set<AppRoleClaim>()
            .Where(roleClaim =>
                roleClaim.RoleId == role.Id &&
                roleClaim.ClaimType == PermissionClaimType)
            .ToListAsync(cancellationToken);

        DateTime utcNow = DateTime.UtcNow;

        if (replaceExisting)
        {
            HashSet<string> desiredPermissions = permissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
            List<AppRoleClaim> staleClaims = existingClaims
                .Where(roleClaim =>
                    !string.IsNullOrWhiteSpace(roleClaim.ClaimValue) &&
                    !desiredPermissions.Contains(roleClaim.ClaimValue))
                .ToList();

            if (staleClaims.Count > 0)
            {
                _context.Set<AppRoleClaim>().RemoveRange(staleClaims);
            }
        }

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
            "Role permissions synchronized. Role: {RoleName}, Permission count: {PermissionCount}",
            role.Name,
            permissions.Count);
    }

    private static IReadOnlyList<string> SelectReviewerPermissions(IReadOnlyList<string> allPermissions)
    {
        string[] exactPermissions =
        {
            "ReviewerEvaluations.Read",
            "ReviewerEvaluations.Write",
            "ReviewerEvaluations.Save",
            "ReviewerEvaluations.Submit"
        };

        return allPermissions
            .Where(permission => exactPermissions.Contains(permission, StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    private static IReadOnlyList<string> SelectAuthorPermissions(IReadOnlyList<string> allPermissions)
    {
        string[] exactPermissions =
        {
            "Submissions.Read",
            "Submissions.Write",
            "Submissions.Add",
            "Submissions.Update",
            "Submissions.Delete"
        };

        return allPermissions
            .Where(permission => exactPermissions.Contains(permission, StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    private static IReadOnlyList<string> SelectRoleManagerPermissions(IReadOnlyList<string> allPermissions)
    {
        string[] prefixes = { "Roles.", "Users." };

        return allPermissions
            .Where(permission => prefixes.Any(prefix => permission.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private async Task<AppUser> EnsureSuperAdminUserAsync()
    {
        string adminEmail = ResolveDefaultAdminEmail();
        string adminPassword = ResolveDefaultAdminPassword();
        string adminName = ResolveConfigurationValue("Authentication:DefaultSuperAdmin:Name", "Seed:AdminName", "Super");
        string adminSurname = ResolveConfigurationValue("Authentication:DefaultSuperAdmin:Surname", "Seed:AdminSurname", "Admin");

        AppUser? user = await _userManager.FindByEmailAsync(adminEmail);

        if (user is not null)
            return user;

        user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = adminEmail,
            NormalizedUserName = adminEmail.ToUpperInvariant(),
            Email = adminEmail,
            NormalizedEmail = adminEmail.ToUpperInvariant(),
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            Name = adminName,
            Surname = adminSurname,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "System"
        };

        TrySetOptionalUserProperty(user, "FirstName", adminName);
        TrySetOptionalUserProperty(user, "LastName", adminSurname);

        IdentityResult result = await _userManager.CreateAsync(user, adminPassword);

        ThrowIfFailed(result, "Default SuperAdmin user could not be created.");

        _logger.LogInformation("Default SuperAdmin user created. Email: {Email}", adminEmail);

        return user;
    }

    private string ResolveDefaultAdminEmail()
    {
        return ResolveConfigurationValue(
            "Authentication:DefaultSuperAdmin:Email",
            "Seed:AdminEmail",
            DefaultAdminEmailFallback);
    }

    private string ResolveDefaultAdminPassword()
    {
        string password = ResolveConfigurationValue(
            "Authentication:DefaultSuperAdmin:Password",
            "Seed:AdminInitialPassword",
            DefaultAdminPasswordFallback);

        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Default SuperAdmin password cannot be empty.");

        return password;
    }

    private string ResolveConfigurationValue(string primaryKey, string secondaryKey, string fallback)
    {
        string? primaryValue = _configuration[primaryKey];

        if (!string.IsNullOrWhiteSpace(primaryValue))
            return primaryValue;

        string? secondaryValue = _configuration[secondaryKey];

        if (!string.IsNullOrWhiteSpace(secondaryValue))
            return secondaryValue;

        return fallback;
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

    private async Task EnsureScenarioUsersAsync(CancellationToken cancellationToken)
    {
        foreach (BackOfficeDemoSeedDefinition.TestUserSeed seed in BackOfficeDemoSeedDefinition.TestUsers)
        {
            AppUser user = await EnsureScenarioUserAsync(seed);

            await EnsureUserRoleAsync(user, seed.RoleName);
            await EnsureOrganizationUserAsync(user, seed, cancellationToken);

            if (string.Equals(seed.RoleName, BackOfficeDemoSeedDefinition.ReviewerRoleName, StringComparison.OrdinalIgnoreCase))
                await EnsureReviewerProfileAsync(user, cancellationToken);
        }
    }

    private async Task<AppUser> EnsureScenarioUserAsync(BackOfficeDemoSeedDefinition.TestUserSeed seed)
    {
        AppUser? user = await _userManager.FindByEmailAsync(seed.Email);

        if (user is null)
        {
            user = new AppUser
            {
                Id = seed.Id,
                UserName = seed.Email,
                NormalizedUserName = seed.Email.ToUpperInvariant(),
                Email = seed.Email,
                NormalizedEmail = seed.Email.ToUpperInvariant(),
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                Name = seed.Name,
                Surname = seed.Surname,
                Institution = seed.Institution,
                Orcid = seed.Orcid,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = BackOfficeDemoSeedDefinition.SystemActor
            };

            IdentityResult createResult = await _userManager.CreateAsync(user, BackOfficeDemoSeedDefinition.DefaultPassword);

            ThrowIfFailed(createResult, $"Scenario user could not be created. Email: {seed.Email}.");

            _logger.LogInformation("Scenario user created. Email: {Email}, Role: {Role}", seed.Email, seed.RoleName);
        }
        else
        {
            user.Name = seed.Name;
            user.Surname = seed.Surname;
            user.Institution = seed.Institution;
            user.Orcid = seed.Orcid;
            user.EmailConfirmed = true;
            user.PhoneNumberConfirmed = true;
            user.DeletedDate = null;
            user.DeletedBy = null;
            user.UpdatedDate = DateTime.UtcNow;
            user.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor;

            await _userManager.UpdateAsync(user);
        }

        return user;
    }

    private async Task EnsureOrganizationUserAsync(
        AppUser user,
        BackOfficeDemoSeedDefinition.TestUserSeed seed,
        CancellationToken cancellationToken)
    {
        OrganizationUser? organizationUser = await _context.Set<OrganizationUser>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(entity =>
                entity.OrganizationId == seed.OrganizationId &&
                entity.UserId == user.Id,
                cancellationToken);

        if (organizationUser is null)
        {
            organizationUser = new OrganizationUser
            {
                Id = Guid.NewGuid(),
                OrganizationId = seed.OrganizationId,
                UserId = user.Id,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = BackOfficeDemoSeedDefinition.SystemActor
            };

            await _context.Set<OrganizationUser>().AddAsync(organizationUser, cancellationToken);
        }
        else
        {
            organizationUser.UpdatedDate = DateTime.UtcNow;
            organizationUser.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor;
        }

        organizationUser.DefaultCongressId = seed.DefaultCongressId;
        organizationUser.IsActive = true;
        organizationUser.DeletedDate = null;
        organizationUser.DeletedBy = null;

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureReviewerProfileAsync(AppUser user, CancellationToken cancellationToken)
    {
        Reviewer? reviewer = await _context.Set<Reviewer>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(entity => entity.UserId == user.Id, cancellationToken);

        if (reviewer is null)
        {
            reviewer = new Reviewer
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = BackOfficeDemoSeedDefinition.SystemActor
            };

            await _context.Set<Reviewer>().AddAsync(reviewer, cancellationToken);
        }
        else
        {
            reviewer.UpdatedDate = DateTime.UtcNow;
            reviewer.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor;
        }

        reviewer.Status = ReviewerStatus.Accepted;
        reviewer.IsActive = true;
        reviewer.DeletedDate = null;
        reviewer.DeletedBy = null;

        await _context.SaveChangesAsync(cancellationToken);
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

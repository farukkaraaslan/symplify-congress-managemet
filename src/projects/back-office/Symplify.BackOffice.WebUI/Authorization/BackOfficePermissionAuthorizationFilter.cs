using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Symplify.BackOffice.WebUI.Authorization;

public sealed class BackOfficePermissionAuthorizationFilter : IAsyncAuthorizationFilter
{
    private const string PermissionClaimType = "Permission";
    private const string SuperAdminRoleName = "SuperAdmin";

    private static readonly HashSet<string> AuthenticatedOnlyControllers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Home",
        "Profile"
    };

    private static readonly HashSet<string> PublicControllers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Auth"
    };

    private static readonly Dictionary<string, string> ControllerSectionAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SubmissionManagement"] = "Submissions",
        ["SubmissionWorkflow"] = "Submissions",
        ["ExhibitionApplications"] = "Submissions",
        ["SubmissionReviewers"] = "Reviewers",
        ["ReviewerUsers"] = "Reviewers",
        ["OrganizationMailConfigurations"] = "Organizations"
    };

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
            return Task.CompletedTask;

        if (context.ActionDescriptor is not ControllerActionDescriptor descriptor)
            return Task.CompletedTask;

        string controller = descriptor.ControllerName;
        string action = descriptor.ActionName;

        if (PublicControllers.Contains(controller))
            return Task.CompletedTask;

        ClaimsPrincipal user = context.HttpContext.User;

        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return Task.CompletedTask;
        }

        if (IsSuperAdmin(user))
            return Task.CompletedTask;

        if (AuthenticatedOnlyControllers.Contains(controller))
            return Task.CompletedTask;

        IReadOnlyCollection<string> requiredPermissions = ResolveRequiredPermissions(controller, action, context.HttpContext.Request.Method);

        if (requiredPermissions.Count == 0)
            return Task.CompletedTask;

        bool allowed = requiredPermissions.Any(permission => HasPermission(user, permission));

        if (!allowed)
            context.Result = new ForbidResult();

        return Task.CompletedTask;
    }

    private static IReadOnlyCollection<string> ResolveRequiredPermissions(string controller, string action, string httpMethod)
    {
        if (string.Equals(controller, "SubmissionManagement", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(controller, "SubmissionWorkflow", StringComparison.OrdinalIgnoreCase))
        {
            return new[] { "Submissions.Admin" };
        }

        string section = ControllerSectionAliases.TryGetValue(controller, out string? alias)
            ? alias
            : controller;

        if (string.Equals(section, "Users", StringComparison.OrdinalIgnoreCase))
            return ResolveUserPermissions(action, httpMethod);

        if (string.Equals(section, "Roles", StringComparison.OrdinalIgnoreCase))
            return ResolveRolePermissions(action, httpMethod);

        string operation = ResolveOperation(action, httpMethod);

        return operation switch
        {
            "Read" => new[] { $"{section}.Admin", $"{section}.Read" },
            "Add" => new[] { $"{section}.Admin", $"{section}.Write", $"{section}.Add" },
            "Update" => new[] { $"{section}.Admin", $"{section}.Write", $"{section}.Update" },
            "Delete" => new[] { $"{section}.Admin", $"{section}.Write", $"{section}.Delete" },
            _ => new[] { $"{section}.Admin", $"{section}.Write" }
        };
    }

    private static IReadOnlyCollection<string> ResolveUserPermissions(string action, string httpMethod)
    {
        if (action.Contains("ResetPassword", StringComparison.OrdinalIgnoreCase))
            return new[] { "Users.Admin", "Users.ResetPassword" };

        if (action.Contains("Roles", StringComparison.OrdinalIgnoreCase))
            return new[] { "Users.Admin", "Users.ManageRoles" };

        if (action.Contains("Claims", StringComparison.OrdinalIgnoreCase))
            return new[] { "Users.Admin", "Users.ManageClaims" };

        if (action.Contains("Blacklist", StringComparison.OrdinalIgnoreCase))
            return new[] { "Users.Admin", "Users.Blacklist" };

        if (action.Contains("Delete", StringComparison.OrdinalIgnoreCase))
            return new[] { "Users.Admin", "Users.Delete" };

        if (action.Contains("Create", StringComparison.OrdinalIgnoreCase))
            return new[] { "Users.Admin", "Users.Add" };

        if (action.Contains("Edit", StringComparison.OrdinalIgnoreCase) || action.Contains("Update", StringComparison.OrdinalIgnoreCase))
            return new[] { "Users.Admin", "Users.Update" };

        return new[] { "Users.Admin", "Users.Read" };
    }

    private static IReadOnlyCollection<string> ResolveRolePermissions(string action, string httpMethod)
    {
        if (action.Contains("Claims", StringComparison.OrdinalIgnoreCase))
            return new[] { "Roles.Admin", "Roles.ManageClaims" };

        string operation = ResolveOperation(action, httpMethod);

        return operation switch
        {
            "Read" => new[] { "Roles.Admin", "Roles.Read" },
            "Add" => new[] { "Roles.Admin", "Roles.Add" },
            "Update" => new[] { "Roles.Admin", "Roles.Update" },
            "Delete" => new[] { "Roles.Admin", "Roles.Delete" },
            _ => new[] { "Roles.Admin", "Roles.Write" }
        };
    }

    private static string ResolveOperation(string action, string httpMethod)
    {
        if (action.StartsWith("Index", StringComparison.OrdinalIgnoreCase) ||
            action.StartsWith("Details", StringComparison.OrdinalIgnoreCase) ||
            action.StartsWith("Manage", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("List", StringComparison.OrdinalIgnoreCase) ||
            action.StartsWith("Get", StringComparison.OrdinalIgnoreCase))
            return "Read";

        if (action.Contains("Create", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("Add", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("Assign", StringComparison.OrdinalIgnoreCase))
            return "Add";

        if (action.Contains("Edit", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("Update", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("Reorder", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("Sync", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("ChangeStatus", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("Save", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("Submit", StringComparison.OrdinalIgnoreCase))
            return "Update";

        if (action.Contains("Delete", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("Remove", StringComparison.OrdinalIgnoreCase))
            return "Delete";

        if (HttpMethods.IsGet(httpMethod))
            return "Read";

        return "Write";
    }

    private static bool IsSuperAdmin(ClaimsPrincipal user)
    {
        return user.IsInRole(SuperAdminRoleName) || HasPermission(user, SuperAdminRoleName);
    }

    private static bool HasPermission(ClaimsPrincipal user, string permission)
    {
        return user.Claims.Any(claim =>
            (string.Equals(claim.Type, PermissionClaimType, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(claim.Type, ClaimTypes.Role, StringComparison.OrdinalIgnoreCase)) &&
            string.Equals(claim.Value, permission, StringComparison.OrdinalIgnoreCase));
    }
}

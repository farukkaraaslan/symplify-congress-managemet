namespace Symplify.BackOffice.Application.Features.Roles.Constants;

public static class RolesOperationClaims
{
    private const string Section = "Roles";

    public const string Admin = $"{Section}.Admin";
    public const string Read = $"{Section}.Read";
    public const string Write = $"{Section}.Write";
    public const string Add = $"{Section}.Add";
    public const string Update = $"{Section}.Update";
    public const string Delete = $"{Section}.Delete";
    public const string ManageClaims = $"{Section}.ManageClaims";
}

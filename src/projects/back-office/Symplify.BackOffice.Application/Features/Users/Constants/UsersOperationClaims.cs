namespace Symplify.BackOffice.Application.Features.Users.Constants;

public static class UsersOperationClaims
{
    private const string Section = "Users";

    public const string Admin = $"{Section}.Admin";
    public const string Read = $"{Section}.Read";
    public const string Write = $"{Section}.Write";
    public const string Add = $"{Section}.Add";
    public const string Update = $"{Section}.Update";
    public const string ResetPassword = $"{Section}.ResetPassword";
    public const string ManageRoles = $"{Section}.ManageRoles";
    public const string ManageClaims = $"{Section}.ManageClaims";
    public const string Blacklist = $"{Section}.Blacklist";
    public const string Delete = $"{Section}.Delete";
}

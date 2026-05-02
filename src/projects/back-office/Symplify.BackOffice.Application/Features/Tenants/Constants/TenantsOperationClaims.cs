namespace Symplify.BackOffice.Application.Features.Tenants.Constants;
public static class TenantsOperationClaims
{
    private const string Section = "Tenants";
    public const string Admin = $"{Section}.Admin";
    public const string Read = $"{Section}.Read";
    public const string Write = $"{Section}.Write";
    public const string Add = $"{Section}.Add";
    public const string Update = $"{Section}.Update";
    public const string Delete = $"{Section}.Delete";
}

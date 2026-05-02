namespace Symplify.BackOffice.Application.Features.Authors.Constants;
public static class AuthorsOperationClaims
{
    private const string Section = "Authors";
    public const string Admin = $"{Section}.Admin";
    public const string Read = $"{Section}.Read";
    public const string Write = $"{Section}.Write";
    public const string Add = $"{Section}.Add";
    public const string Update = $"{Section}.Update";
    public const string Delete = $"{Section}.Delete";
}

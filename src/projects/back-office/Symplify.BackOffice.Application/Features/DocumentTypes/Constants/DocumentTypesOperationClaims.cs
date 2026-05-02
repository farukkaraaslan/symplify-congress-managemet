namespace Symplify.BackOffice.Application.Features.DocumentTypes.Constants;
public static class DocumentTypesOperationClaims
{
    private const string Section = "DocumentTypes";
    public const string Admin = $"{Section}.Admin";
    public const string Read = $"{Section}.Read";
    public const string Write = $"{Section}.Write";
    public const string Add = $"{Section}.Add";
    public const string Update = $"{Section}.Update";
    public const string Delete = $"{Section}.Delete";
}

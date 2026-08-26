namespace Symplify.BackOffice.Application.Features.PaymentDocuments.Constants;
public static class PaymentDocumentsOperationClaims
{
    private const string Section = "PaymentDocuments";
    public const string Admin = $"{Section}.Admin";
    public const string Read = $"{Section}.Read";
    public const string Write = $"{Section}.Write";
    public const string Add = $"{Section}.Add";
    public const string Update = $"{Section}.Update";
    public const string Delete = $"{Section}.Delete";
}

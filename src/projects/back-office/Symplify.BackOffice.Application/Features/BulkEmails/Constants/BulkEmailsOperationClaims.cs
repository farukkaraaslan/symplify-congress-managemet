namespace Symplify.BackOffice.Application.Features.BulkEmails.Constants;

public static class BulkEmailsOperationClaims
{
    private const string Section = "BulkEmails";

    public const string Admin = $"{Section}.Admin";
    public const string Read = $"{Section}.Read";
    public const string Write = $"{Section}.Write";
    public const string Add = $"{Section}.Add";
}

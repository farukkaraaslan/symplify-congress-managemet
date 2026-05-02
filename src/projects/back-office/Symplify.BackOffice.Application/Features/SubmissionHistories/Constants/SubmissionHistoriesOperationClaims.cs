namespace Symplify.BackOffice.Application.Features.SubmissionHistories.Constants;
public static class SubmissionHistoriesOperationClaims
{
    private const string Section = "SubmissionHistories";
    public const string Admin = $"{Section}.Admin";
    public const string Read = $"{Section}.Read";
    public const string Write = $"{Section}.Write";
    public const string Add = $"{Section}.Add";
    public const string Update = $"{Section}.Update";
    public const string Delete = $"{Section}.Delete";
}

namespace Symplify.BackOffice.Application.Features.AbstractBook.Constants;

public static class AbstractBookOperationClaims
{
    // Kept aligned with Program Management until a dedicated permission is introduced.
    public static readonly string[] AdminOnly = { "Submissions.Admin" };
}

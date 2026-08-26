namespace Symplify.BackOffice.Application.Features.CongressPaymentPlans.Constants;

public static class CongressPaymentPlanCategories
{
    public const string Participation = "Participation";
    public const string SecondSubmission = "SecondSubmission";
    public const string Listener = "Listener";
    public const string Student = "Student";
    public const string Other = "Other";

    private static readonly HashSet<string> Values = new(StringComparer.OrdinalIgnoreCase)
    {
        Participation,
        SecondSubmission,
        Listener,
        Student,
        Other
    };

    public static IReadOnlyCollection<string> AllValues => Values;

    public static bool IsValid(string? value)
        => !string.IsNullOrWhiteSpace(value) && Values.Contains(value.Trim());

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Participation;

        string normalizedValue = value.Trim();

        return Values.FirstOrDefault(item => string.Equals(item, normalizedValue, StringComparison.OrdinalIgnoreCase))
            ?? Other;
    }
}

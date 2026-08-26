namespace Symplify.BackOffice.Application.Features.CongressPaymentPlans.Constants;

public static class CongressPaymentPlanAudienceTypes
{
    public const string All = "All";
    public const string Domestic = "Domestic";
    public const string International = "International";

    private static readonly HashSet<string> Values = new(StringComparer.OrdinalIgnoreCase)
    {
        All,
        Domestic,
        International
    };

    public static IReadOnlyCollection<string> AllValues => Values;

    public static bool IsValid(string? value)
        => !string.IsNullOrWhiteSpace(value) && Values.Contains(value.Trim());

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return All;

        string normalizedValue = value.Trim();

        if (string.Equals(normalizedValue, Domestic, StringComparison.OrdinalIgnoreCase))
            return Domestic;

        if (string.Equals(normalizedValue, International, StringComparison.OrdinalIgnoreCase))
            return International;

        return All;
    }
}

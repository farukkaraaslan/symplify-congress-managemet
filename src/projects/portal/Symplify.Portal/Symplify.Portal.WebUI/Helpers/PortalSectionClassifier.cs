using System.Globalization;
using System.Text;
using Symplify.Portal.WebUI.Models.PublicSite;

namespace Symplify.Portal.WebUI.Helpers;

public static class PortalSectionClassifier
{
    private static readonly string[] PaymentBindingKeyTokens =
    {
        "kayit-kosullari",
        "kayit-sartlari",
        "registration-conditions",
        "registration-terms",
        "payment",
        "payments",
        "odeme"
    };

    private static readonly string[] PaymentTitleTokens =
    {
        "kayit kosullari",
        "kayit sartlari",
        "registration conditions",
        "registration terms",
        "payment conditions",
        "payment terms",
        "odeme"
    };

    public static bool IsPaymentSection(PublicSectionResponse? section)
    {
        if (section is null)
        {
            return false;
        }

        string bindingKey = Normalize(section.BindingKey).Replace(' ', '-');
        if (PaymentBindingKeyTokens.Any(token =>
                bindingKey.Equals(token, StringComparison.OrdinalIgnoreCase)
                || bindingKey.Contains(token, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        string title = Normalize(section.Title);
        return PaymentTitleTokens.Any(token =>
            title.Equals(token, StringComparison.OrdinalIgnoreCase)
            || title.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (char character in decomposed)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(character switch
            {
                'ı' => 'i',
                'ş' => 's',
                'ğ' => 'g',
                'ü' => 'u',
                'ö' => 'o',
                'ç' => 'c',
                '_' or '/' or '\\' => '-',
                _ => character
            });
        }

        return string.Join(' ', builder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}

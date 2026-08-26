using System.Net;
using Symplify.BackOffice.Application.Features.Auth.Constants;
using Symplify.BackOffice.Application.Services.Email;
using Symplify.BackOffice.WebUI.Localization;

namespace Symplify.BackOffice.WebUI.Services.Auth;

public static class AuthEmailTemplateBuilder
{
    public static BackOfficeEmailMessage BuildEmailConfirmationMessage(
        IBackOfficeViewLocalizer localizer,
        Guid organizationId,
        string toEmail,
        string displayName,
        string confirmationUrl)
    {
        return BuildMessage(
            localizer,
            AuthResourceKeys.EmailConfirmationSubject,
            AuthResourceKeys.EmailConfirmationBodyHtml,
            organizationId,
            toEmail,
            displayName,
            confirmationUrl);
    }

    public static BackOfficeEmailMessage BuildResetPasswordMessage(
        IBackOfficeViewLocalizer localizer,
        Guid organizationId,
        string toEmail,
        string displayName,
        string resetUrl)
    {
        return BuildMessage(
            localizer,
            AuthResourceKeys.ResetPasswordSubject,
            AuthResourceKeys.ResetPasswordBodyHtml,
            organizationId,
            toEmail,
            displayName,
            resetUrl);
    }

    private static BackOfficeEmailMessage BuildMessage(
        IBackOfficeViewLocalizer localizer,
        string subjectKey,
        string bodyKey,
        Guid organizationId,
        string toEmail,
        string displayName,
        string actionUrl)
    {
        string subject = localizer.GetStringValueSafe(subjectKey);
        string template = localizer.GetStringValueSafe(bodyKey);

        string safeDisplayName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(displayName) ? toEmail : displayName);
        string safeActionUrl = WebUtility.HtmlEncode(actionUrl);

        string htmlBody = string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            template,
            safeDisplayName,
            safeActionUrl);

        return new BackOfficeEmailMessage
        {
            OrganizationId = organizationId,
            ToEmail = toEmail,
            ToName = displayName,
            Subject = subject,
            HtmlBody = htmlBody
        };
    }
}

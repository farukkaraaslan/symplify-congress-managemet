using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Symplify.BackOffice.Application.Features.Auth.Constants;
using Symplify.BackOffice.WebUI.Localization;
using Symplify.BackOffice.WebUI.Models.Auth;

namespace Symplify.BackOffice.WebUI.Services.Auth;

public static class AuthModelStateLocalizer
{
    public static bool ValidateLogin(LoginViewModel model, ModelStateDictionary modelState, IBackOfficeViewLocalizer localizer)
    {
        if (string.IsNullOrWhiteSpace(model.Email))
            Add(modelState, nameof(model.Email), AuthMessages.EmailRequired, localizer);
        else if (!IsEmail(model.Email))
            Add(modelState, nameof(model.Email), AuthMessages.EmailInvalid, localizer);

        if (string.IsNullOrWhiteSpace(model.Password))
            Add(modelState, nameof(model.Password), AuthMessages.PasswordRequired, localizer);

        return modelState.IsValid;
    }

    public static bool ValidateRegister(RegisterViewModel model, ModelStateDictionary modelState, IBackOfficeViewLocalizer localizer)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            Add(modelState, nameof(model.Name), AuthMessages.NameRequired, localizer);

        if (string.IsNullOrWhiteSpace(model.Surname))
            Add(modelState, nameof(model.Surname), AuthMessages.SurnameRequired, localizer);

        if (string.IsNullOrWhiteSpace(model.Institution))
            Add(modelState, nameof(model.Institution), AuthMessages.InstitutionRequired, localizer);

        if (!model.TitleId.HasValue || model.TitleId.Value == Guid.Empty)
            Add(modelState, nameof(model.TitleId), AuthMessages.TitleRequired, localizer);

        if (!model.OrganizationId.HasValue || model.OrganizationId.Value == Guid.Empty)
            Add(modelState, nameof(model.OrganizationId), AuthMessages.OrganizationRequired, localizer);

        if (!model.CountryId.HasValue || model.CountryId.Value == Guid.Empty)
            Add(modelState, nameof(model.CountryId), AuthMessages.CountryRequired, localizer);

        ValidatePhoneNumber(model, modelState, localizer);

        ValidateEmail(model.Email, nameof(model.Email), modelState, localizer);
        ValidatePassword(model.Password, nameof(model.Password), modelState, localizer);

        if (string.IsNullOrWhiteSpace(model.ConfirmPassword))
            Add(modelState, nameof(model.ConfirmPassword), AuthMessages.ConfirmPasswordRequired, localizer);
        else if (!string.Equals(model.Password, model.ConfirmPassword, StringComparison.Ordinal))
            Add(modelState, nameof(model.ConfirmPassword), AuthMessages.PasswordsDoNotMatch, localizer);

        if (!model.AcceptTerms)
            Add(modelState, nameof(model.AcceptTerms), AuthMessages.TermsRequired, localizer);

        return modelState.IsValid;
    }

    public static bool ValidateForgotPassword(ForgotPasswordViewModel model, ModelStateDictionary modelState, IBackOfficeViewLocalizer localizer)
    {
        ValidateEmail(model.Email, nameof(model.Email), modelState, localizer);
        return modelState.IsValid;
    }

    public static bool ValidateResetPassword(ResetPasswordViewModel model, ModelStateDictionary modelState, IBackOfficeViewLocalizer localizer)
    {
        ValidateEmail(model.Email, nameof(model.Email), modelState, localizer);

        if (string.IsNullOrWhiteSpace(model.Token))
            Add(modelState, nameof(model.Token), AuthMessages.TokenRequired, localizer);

        ValidatePassword(model.Password, nameof(model.Password), modelState, localizer);

        if (string.IsNullOrWhiteSpace(model.ConfirmPassword))
            Add(modelState, nameof(model.ConfirmPassword), AuthMessages.ConfirmPasswordRequired, localizer);
        else if (!string.Equals(model.Password, model.ConfirmPassword, StringComparison.Ordinal))
            Add(modelState, nameof(model.ConfirmPassword), AuthMessages.PasswordsDoNotMatch, localizer);

        return modelState.IsValid;
    }

    public static void AddBusinessError(ModelStateDictionary modelState, string messageOrKey, IBackOfficeViewLocalizer localizer)
    {
        Add(modelState, string.Empty, messageOrKey, localizer);
    }

    private static void ValidatePhoneNumber(RegisterViewModel model, ModelStateDictionary modelState, IBackOfficeViewLocalizer localizer)
    {
        string normalized = NormalizePhoneNumber(model.PhoneNumber, model.PhoneDialCode);
        model.PhoneNumber = normalized;

        if (string.IsNullOrWhiteSpace(normalized))
        {
            Add(modelState, nameof(model.PhoneNumber), AuthMessages.PhoneRequired, localizer);
            return;
        }

        if (!Regex.IsMatch(normalized, @"^\+[1-9]\d{6,14}$"))
            Add(modelState, nameof(model.PhoneNumber), AuthMessages.PhoneInvalid, localizer);
    }

    private static string NormalizePhoneNumber(string? phoneNumber, string? dialCode)
    {
        string raw = phoneNumber?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        string digits = new(raw.Where(char.IsDigit).ToArray());
        if (raw.StartsWith("+", StringComparison.Ordinal))
            return digits.Length == 0 ? string.Empty : $"+{digits}";

        string dialDigits = new((dialCode ?? string.Empty).Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(dialDigits))
            return digits;

        if (digits.StartsWith(dialDigits, StringComparison.Ordinal))
            return $"+{digits}";

        digits = digits.TrimStart('0');
        return digits.Length == 0 ? string.Empty : $"+{dialDigits}{digits}";
    }

    private static void ValidateEmail(string email, string fieldName, ModelStateDictionary modelState, IBackOfficeViewLocalizer localizer)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            Add(modelState, fieldName, AuthMessages.EmailRequired, localizer);
            return;
        }

        if (!IsEmail(email))
            Add(modelState, fieldName, AuthMessages.EmailInvalid, localizer);
    }

    private static void ValidatePassword(string password, string fieldName, ModelStateDictionary modelState, IBackOfficeViewLocalizer localizer)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            Add(modelState, fieldName, AuthMessages.PasswordRequired, localizer);
            return;
        }

        bool isValid = password.Length >= 8 &&
            password.Any(char.IsUpper) &&
            password.Any(char.IsDigit) &&
            password.Any(character => !char.IsLetterOrDigit(character));

        if (!isValid)
            Add(modelState, fieldName, AuthMessages.PasswordPolicy, localizer);
    }

    private static bool IsEmail(string value)
    {
        try
        {
            _ = new System.Net.Mail.MailAddress(value.Trim());
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void Add(ModelStateDictionary modelState, string key, string resourceKey, IBackOfficeViewLocalizer localizer)
    {
        modelState.AddModelError(key, ResolveMessage(resourceKey, localizer));
    }

    private static string ResolveMessage(string resourceKey, IBackOfficeViewLocalizer localizer)
    {
        string localized = localizer.GetStringValueSafe(resourceKey);
        if (!string.Equals(localized, resourceKey, StringComparison.Ordinal))
            return localized;

        bool isEnglish = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("en", StringComparison.OrdinalIgnoreCase);

        return resourceKey switch
        {
            AuthMessages.PhoneRequired => isEnglish
                ? "Phone number is required."
                : "Telefon numarası zorunludur.",
            AuthMessages.PhoneInvalid => isEnglish
                ? "Enter a valid phone number including the country code."
                : "Telefon numarasını ülke kodu ile birlikte geçerli formatta giriniz.",
            _ => resourceKey
        };
    }
}

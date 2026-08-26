namespace Symplify.BackOffice.Application.Features.Auth.Constants;

public static class AuthMessages
{
    public const string InvalidCredentials = "BackOffice.Auth.Validation.InvalidCredentials";

    // Eski AuthBusinessRules kullanımını bozmamak için alias.
    // Statik mesaj değil, DB localization key döner.
    public const string UserNotFoundOrPasswordInvalid = InvalidCredentials;

    public const string AccountLocked = "BackOffice.Auth.Validation.AccountLocked";
    public const string AccountBlacklisted = "BackOffice.Auth.Validation.AccountBlacklisted";
    public const string EmailNotConfirmed = "BackOffice.Auth.Validation.EmailNotConfirmed";
    public const string EmailAlreadyRegistered = "BackOffice.Auth.Validation.EmailAlreadyRegistered";
    public const string RegisterFailed = "BackOffice.Auth.Validation.RegisterFailed";
    public const string ResetPasswordFailed = "BackOffice.Auth.Validation.ResetPasswordFailed";
    public const string ConfirmEmailFailed = "BackOffice.Auth.Validation.ConfirmEmailFailed";
    public const string AuthorRoleMissing = "BackOffice.Auth.Validation.AuthorRoleMissing";
    public const string CongressRequired = "BackOffice.Auth.Validation.CongressRequired";
    public const string CongressNotFound = "BackOffice.Auth.Validation.CongressNotFound";
    public const string OrganizationRequired = "BackOffice.Auth.Validation.OrganizationRequired";
    public const string OrganizationNotFound = "BackOffice.Auth.Validation.OrganizationNotFound";
    public const string OrganizationMembershipRequired = "BackOffice.Auth.Validation.OrganizationMembershipRequired";
    public const string TermsRequired = "BackOffice.Auth.Validation.TermsRequired";

    public const string NameRequired = "BackOffice.Auth.Validation.NameRequired";
    public const string SurnameRequired = "BackOffice.Auth.Validation.SurnameRequired";
    public const string InstitutionRequired = "BackOffice.Auth.Validation.InstitutionRequired";
    public const string TitleRequired = "BackOffice.Auth.Validation.TitleRequired";
    public const string CountryRequired = "BackOffice.Auth.Validation.CountryRequired";
    public const string PhoneRequired = "BackOffice.Auth.Validation.PhoneRequired";
    public const string PhoneInvalid = "BackOffice.Auth.Validation.PhoneInvalid";
    public const string EmailRequired = "BackOffice.Auth.Validation.EmailRequired";
    public const string EmailInvalid = "BackOffice.Auth.Validation.EmailInvalid";
    public const string PasswordRequired = "BackOffice.Auth.Validation.PasswordRequired";
    public const string PasswordPolicy = "BackOffice.Auth.Validation.PasswordPolicy";
    public const string ConfirmPasswordRequired = "BackOffice.Auth.Validation.ConfirmPasswordRequired";
    public const string PasswordsDoNotMatch = "BackOffice.Auth.Validation.PasswordsDoNotMatch";
    public const string TokenRequired = "BackOffice.Auth.Validation.TokenRequired";
}
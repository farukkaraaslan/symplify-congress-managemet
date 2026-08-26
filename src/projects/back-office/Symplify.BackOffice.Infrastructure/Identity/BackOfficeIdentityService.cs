using Core.CrossCuttingConcerns.Exceptions.Types;
using Microsoft.AspNetCore.Identity;
using Symplify.BackOffice.Application.Features.Auth.Constants;
using Symplify.BackOffice.Application.Common.Text;
using Symplify.BackOffice.Application.Services.Authentication;
using Symplify.BackOffice.Domain.Identity;

namespace Symplify.BackOffice.Infrastructure.Identity;

public sealed class BackOfficeIdentityService : IBackOfficeIdentityService
{
    private const string AuthorRoleName = "Author";

    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;

    public BackOfficeIdentityService(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<AuthenticatedUserDto> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new BusinessException(AuthMessages.InvalidCredentials);

        AppUser? user = await _userManager.FindByEmailAsync(NormalizeEmail(email));

        if (user is null || user.DeletedDate is not null)
            throw new BusinessException(AuthMessages.InvalidCredentials);

        if (user.IsBlacklisted)
            throw new BusinessException(AuthMessages.AccountBlacklisted);

        if (await _userManager.IsLockedOutAsync(user))
            throw new BusinessException(AuthMessages.AccountLocked);

        bool isPasswordValid = await _userManager.CheckPasswordAsync(user, password);

        if (!isPasswordValid)
        {
            if (user.LockoutEnabled)
                await _userManager.AccessFailedAsync(user);

            throw new BusinessException(AuthMessages.InvalidCredentials);
        }

        if (!await _userManager.IsEmailConfirmedAsync(user))
            throw new BusinessException(AuthMessages.EmailNotConfirmed);

        if (user.AccessFailedCount > 0)
            await _userManager.ResetAccessFailedCountAsync(user);

        IList<string> roles = await _userManager.GetRolesAsync(user);

        return new AuthenticatedUserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = ResolveDisplayName(user),
            OperationClaims = roles
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
        };
    }

    public async Task<RegisteredAuthorUserDto> RegisterAuthorAsync(
        RegisterAuthorUserRequest request,
        CancellationToken cancellationToken = default)
    {
        string normalizedEmail = NormalizeEmail(request.Email);

        AppUser? existingUser = await _userManager.FindByEmailAsync(normalizedEmail);
        if (existingUser is not null && existingUser.DeletedDate is null)
            throw new BusinessException(AuthMessages.EmailAlreadyRegistered);

        if (!await _roleManager.RoleExistsAsync(AuthorRoleName))
            throw new BusinessException(AuthMessages.AuthorRoleMissing);

        AppUser user = new()
        {
            Id = Guid.NewGuid(),
            UserName = normalizedEmail,
            Email = normalizedEmail,
            EmailConfirmed = false,
            Name = BackOfficeTextNormalizer.NormalizeRequiredPersonFirstName(request.Name),
            Surname = BackOfficeTextNormalizer.NormalizeRequiredPersonSurname(request.Surname),
            Institution = BackOfficeTextNormalizer.NormalizeInstitution(request.Institution),
            TitleId = request.TitleId,
            CountryId = request.CountryId,
            StateId = request.StateId,
            PhoneNumber = NormalizePhoneNumber(request.PhoneNumber),
            PhoneNumberConfirmed = false,
            IsBlacklisted = false,
            LockoutEnabled = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "SelfRegistration"
        };

        IdentityResult createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            throw new BusinessException(ResolveIdentityErrorMessage(createResult, AuthMessages.RegisterFailed));

        IdentityResult roleResult = await _userManager.AddToRoleAsync(user, AuthorRoleName);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            throw new BusinessException(ResolveIdentityErrorMessage(roleResult, AuthMessages.RegisterFailed));
        }

        string confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        return new RegisteredAuthorUserDto
        {
            UserId = user.Id,
            Email = user.Email ?? normalizedEmail,
            DisplayName = ResolveDisplayName(user),
            EmailConfirmationToken = confirmationToken
        };
    }

    public async Task<PasswordResetTokenDto> GeneratePasswordResetTokenAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return new PasswordResetTokenDto
            {
                TokenGenerated = false,
                Email = string.Empty
            };
        }

        string normalizedEmail = NormalizeEmail(email);
        AppUser? user = await _userManager.FindByEmailAsync(normalizedEmail);

        if (user is null ||
            user.DeletedDate is not null ||
            user.IsBlacklisted ||
            !await _userManager.IsEmailConfirmedAsync(user))
        {
            return new PasswordResetTokenDto
            {
                TokenGenerated = false,
                Email = normalizedEmail
            };
        }

        string token = await _userManager.GeneratePasswordResetTokenAsync(user);

        return new PasswordResetTokenDto
        {
            UserId = user.Id,
            TokenGenerated = true,
            Email = user.Email ?? normalizedEmail,
            Token = token,
            DisplayName = ResolveDisplayName(user)
        };
    }

    public async Task ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            throw new BusinessException(AuthMessages.ResetPasswordFailed);

        AppUser? user = await _userManager.FindByEmailAsync(NormalizeEmail(email));

        if (user is null || user.DeletedDate is not null || user.IsBlacklisted)
            throw new BusinessException(AuthMessages.ResetPasswordFailed);

        IdentityResult result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
            throw new BusinessException(ResolveIdentityErrorMessage(result, AuthMessages.ResetPasswordFailed));
    }

    public async Task ConfirmEmailAsync(
        string email,
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            throw new BusinessException(AuthMessages.ConfirmEmailFailed);

        AppUser? user = await _userManager.FindByEmailAsync(NormalizeEmail(email));

        if (user is null || user.DeletedDate is not null || user.IsBlacklisted)
            throw new BusinessException(AuthMessages.ConfirmEmailFailed);

        if (await _userManager.IsEmailConfirmedAsync(user))
            return;

        IdentityResult result = await _userManager.ConfirmEmailAsync(user, token);

        if (!result.Succeeded)
            throw new BusinessException(ResolveIdentityErrorMessage(result, AuthMessages.ConfirmEmailFailed));
    }

    private static string ResolveDisplayName(AppUser user)
    {
        string fullName = $"{user.Name} {user.Surname}".Trim();

        if (!string.IsNullOrWhiteSpace(fullName))
            return fullName;

        return user.Email ?? user.UserName ?? string.Empty;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string? NormalizeNullable(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizePhoneNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        string trimmed = value.Trim();
        string normalized = trimmed.StartsWith("+", StringComparison.Ordinal)
            ? "+" + new string(trimmed[1..].Where(char.IsDigit).ToArray())
            : new string(trimmed.Where(char.IsDigit).ToArray());

        return normalized.Length == 0 ? null : normalized;
    }

    private static string ResolveIdentityErrorMessage(IdentityResult result, string fallbackKey)
    {
        IdentityError? firstError = result.Errors.FirstOrDefault();
        return string.IsNullOrWhiteSpace(firstError?.Code)
            ? fallbackKey
            : $"BackOffice.Auth.Identity.{firstError.Code}";
    }
}

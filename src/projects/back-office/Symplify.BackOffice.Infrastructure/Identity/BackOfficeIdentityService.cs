using Core.CrossCuttingConcerns.Exceptions.Types;
using Microsoft.AspNetCore.Identity;
using Symplify.BackOffice.Application.Services.Authentication;
using Symplify.BackOffice.Domain.Identity;

namespace Symplify.BackOffice.Infrastructure.Identity;

public sealed class BackOfficeIdentityService : IBackOfficeIdentityService
{
    private readonly UserManager<AppUser> _userManager;

    public BackOfficeIdentityService(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<AuthenticatedUserDto> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new BusinessException("User not found or password is invalid.");

        AppUser? user = await _userManager.FindByEmailAsync(email.Trim());

        if (user is null)
            throw new BusinessException("User not found or password is invalid.");

        bool isPasswordValid = await _userManager.CheckPasswordAsync(user, password);

        if (!isPasswordValid)
            throw new BusinessException("User not found or password is invalid.");

        return new AuthenticatedUserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = ResolveDisplayName(user),
            OperationClaims = Array.Empty<string>()
        };
    }

    private static string ResolveDisplayName(AppUser user)
    {
        string? name = GetOptionalStringProperty(user, "Name");
        string? surname = GetOptionalStringProperty(user, "Surname");

        string fullName = $"{name} {surname}".Trim();

        if (!string.IsNullOrWhiteSpace(fullName))
            return fullName;

        return user.Email ?? user.UserName ?? string.Empty;
    }

    private static string? GetOptionalStringProperty(AppUser user, string propertyName)
    {
        var property = typeof(AppUser).GetProperty(propertyName);

        if (property is null || property.PropertyType != typeof(string))
            return null;

        return property.GetValue(user)?.ToString();
    }
}
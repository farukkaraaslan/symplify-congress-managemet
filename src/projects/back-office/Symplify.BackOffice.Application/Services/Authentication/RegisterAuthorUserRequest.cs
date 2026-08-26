namespace Symplify.BackOffice.Application.Services.Authentication;

public sealed class RegisterAuthorUserRequest
{
    public string Name { get; set; } = string.Empty;

    public string Surname { get; set; } = string.Empty;

    public string? Institution { get; set; }

    public Guid? TitleId { get; set; }

    public Guid? CountryId { get; set; }

    public Guid? StateId { get; set; }

    public string? PhoneNumber { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

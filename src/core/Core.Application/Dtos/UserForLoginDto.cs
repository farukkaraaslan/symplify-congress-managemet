namespace Core.Application.Dtos;

public class UserForLoginDto : IDto
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public string? AuthenticatorCode { get; init; }
}

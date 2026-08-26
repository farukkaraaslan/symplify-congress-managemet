namespace Core.Security.JWT;

public class TokenOptions
{
    public required string Audience { get; init; }
    public required string Issuer { get; init; }
    public int AccessTokenExpiration { get; init; }
    public required string SecurityKey { get; init; }
    public int RefreshTokenTTL { get; init; }
}

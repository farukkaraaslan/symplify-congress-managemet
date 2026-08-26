namespace Core.Security.JWT;

public class AccessToken
{
    public required string Token { get; init; }
    public DateTime Expiration { get; init; }
}

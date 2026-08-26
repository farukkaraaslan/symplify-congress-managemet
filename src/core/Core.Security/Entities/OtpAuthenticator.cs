using Core.Persistence.Repositories;

namespace Core.Security.Entities;

public class OtpAuthenticator : Entity<int>
{
    public int UserId { get; set; }
    public required byte[] SecretKey { get; set; }
    public bool IsVerified { get; set; }
    public virtual User User { get; set; } = null!;

    public OtpAuthenticator() { }

    public OtpAuthenticator(int id, int userId, byte[] secretKey, bool isVerified)
        : this()
    {
        Id = id;
        UserId = userId;
        SecretKey = secretKey;
        IsVerified = isVerified;
    }
}

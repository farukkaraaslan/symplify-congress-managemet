using OtpNet;

namespace Core.Security.OtpAuthenticator.OtpNet;

public class OtpNetOtpAuthenticatorHelper : IOtpAuthenticatorHelper
{
    public byte[] GenerateSecretKey()
    {
        byte[] key = KeyGeneration.GenerateRandomKey(20);

        string base32String = Base32Encoding.ToString(key);
        byte[] base32Bytes = Base32Encoding.ToBytes(base32String);

        return base32Bytes;
    }

    public string ConvertSecretKeyToString(byte[] secretKey)
    {
        string base32String = Base32Encoding.ToString(secretKey);
        return base32String;
    }

    public bool VerifyCode(byte[] secretKey, string code)
    {
        Totp totp = new(secretKey);

        string totpCode = totp.ComputeTotp(DateTime.UtcNow);

        return totpCode == code;
    }
}

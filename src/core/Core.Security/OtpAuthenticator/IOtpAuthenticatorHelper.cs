namespace Core.Security.OtpAuthenticator;

public interface IOtpAuthenticatorHelper
{
    public byte[] GenerateSecretKey();
    public string ConvertSecretKeyToString(byte[] secretKey);
    public bool VerifyCode(byte[] secretKey, string code);
}

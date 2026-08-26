using System.Security.Cryptography;

namespace Core.Security.EmailAuthenticator;

public class EmailAuthenticatorHelper : IEmailAuthenticatorHelper
{
    public string CreateEmailActivationKey()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public string CreateEmailActivationCode()
    {
        return RandomNumberGenerator
            .GetInt32(Convert.ToInt32(Math.Pow(x: 10, y: 6)))
            .ToString()
            .PadLeft(totalWidth: 6, paddingChar: '0');
    }
}

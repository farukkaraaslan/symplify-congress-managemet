namespace Core.Security.EmailAuthenticator;

public interface IEmailAuthenticatorHelper
{
    public string CreateEmailActivationKey();
    public string CreateEmailActivationCode();
}

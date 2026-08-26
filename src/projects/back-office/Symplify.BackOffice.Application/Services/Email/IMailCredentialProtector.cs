namespace Symplify.BackOffice.Application.Services.Email;

public interface IMailCredentialProtector
{
    string Protect(string plainText);

    string Unprotect(string protectedText);
}

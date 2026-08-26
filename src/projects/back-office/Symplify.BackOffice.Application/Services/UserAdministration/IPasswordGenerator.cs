namespace Symplify.BackOffice.Application.Services.UserAdministration;

public interface IPasswordGenerator
{
    string Generate(int length = 14);
}

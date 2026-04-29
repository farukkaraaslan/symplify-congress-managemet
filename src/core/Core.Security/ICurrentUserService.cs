namespace Core.Utilities;

public interface ICurrentUserService
{
    string GetUserId();

    bool IsInRole(string roleName);
}


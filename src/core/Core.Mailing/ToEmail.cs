namespace Core.Mailing;

public class ToEmail
{
    public required string Email { get; init; }
    public required string FullName { get; init; }

    public ToEmail() { }

    public ToEmail(string email, string fullName)
    {
        Email = email;
        FullName = fullName;
    }
}

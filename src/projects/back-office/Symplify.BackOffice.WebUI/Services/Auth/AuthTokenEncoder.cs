using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Symplify.BackOffice.WebUI.Services.Auth;

public static class AuthTokenEncoder
{
    public static string Encode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(value));
    }

    public static string Decode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        try
        {
            return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(value));
        }
        catch
        {
            return value;
        }
    }
}

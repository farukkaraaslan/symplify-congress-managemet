namespace Symplify.Portal.WebUI.Services.PublicSite;

public sealed class PublicSiteApiException : Exception
{
    public int StatusCode { get; }

    public PublicSiteApiException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}

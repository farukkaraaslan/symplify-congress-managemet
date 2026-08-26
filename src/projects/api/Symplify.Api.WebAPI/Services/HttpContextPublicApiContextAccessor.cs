using Symplify.Api.Application.Features.PublicSite.Contexts;

namespace Symplify.Api.WebAPI.Services;

public sealed class HttpContextPublicApiContextAccessor : IPublicApiContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextPublicApiContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public PublicApiContext? Current
    {
        get
        {
            HttpContext? httpContext = _httpContextAccessor.HttpContext;

            if (httpContext is null)
                return null;

            return httpContext.Items.TryGetValue(PublicApiContext.HttpContextItemKey, out object? value)
                ? value as PublicApiContext
                : null;
        }
    }
}

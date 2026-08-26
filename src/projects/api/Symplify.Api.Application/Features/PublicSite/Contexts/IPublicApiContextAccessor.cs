namespace Symplify.Api.Application.Features.PublicSite.Contexts;

public interface IPublicApiContextAccessor
{
    PublicApiContext? Current { get; }
}

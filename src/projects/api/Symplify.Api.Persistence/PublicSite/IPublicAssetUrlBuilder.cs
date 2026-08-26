namespace Symplify.Api.Persistence.PublicSite;

public interface IPublicAssetUrlBuilder
{
    string? Build(string? pathOrObjectName, string? bucketName = null);
}

namespace Symplify.BackOffice.Application.Services.Urls;

/// <summary>
/// Browser, mail ve dış paylaşımlarda kullanılacak tek canonical public URL kaynağıdır.
/// Kaynak environment variable:
/// PUBLIC_ASSET_BASE_URL=https://globalworldiletisim.com
/// </summary>
public interface IPublicUrlService
{
    string BaseUrl { get; }

    string Build(string relativePath);
}

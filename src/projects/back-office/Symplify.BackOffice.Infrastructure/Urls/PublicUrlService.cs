using Microsoft.Extensions.Configuration;
using Symplify.BackOffice.Application.Services.Urls;

namespace Symplify.BackOffice.Infrastructure.Urls;

/// <summary>
/// Symplify dış dünyaya URL üretirken tek kaynağı kullanır.
///
/// Mevcut deployment ile geriye uyumluluk için environment variable adı
/// PUBLIC_ASSET_BASE_URL olarak korunmuştur; artık yalnızca asset değil,
/// uygulamanın canonical public base URL'idir.
/// </summary>
public sealed class PublicUrlService : IPublicUrlService
{
    public const string EnvironmentVariableName = "PUBLIC_ASSET_BASE_URL";
    public const string DefaultBaseUrl = "https://globalworldiletisim.com";

    public PublicUrlService(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string configuredValue =
            configuration[EnvironmentVariableName]?.Trim()
            ?? string.Empty;

        string candidate = string.IsNullOrWhiteSpace(configuredValue)
            ? DefaultBaseUrl
            : configuredValue;

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) ||
            !string.IsNullOrWhiteSpace(uri.UserInfo) ||
            string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new InvalidOperationException(
                $"{EnvironmentVariableName} geçerli bir absolute HTTP/HTTPS URL olmalıdır.");
        }

        // Canonical public base URL yalnızca origin olarak tutulur.
        // Query/fragment deployment hatalarını sessizce taşımayalım.
        if (!string.IsNullOrWhiteSpace(uri.Query) ||
            !string.IsNullOrWhiteSpace(uri.Fragment))
        {
            throw new InvalidOperationException(
                $"{EnvironmentVariableName} query string veya fragment içeremez.");
        }

        BaseUrl = candidate.TrimEnd('/');
    }

    public string BaseUrl { get; }

    public string Build(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || relativePath == "/")
            return BaseUrl;

        string normalized = relativePath.Trim().Replace('\\', '/');

        // Url.Action ve uygulamadaki route builder'lar çoğunlukla:
        //
        // /tr-TR/submissions/details/...
        //
        // şeklinde root-relative path döndürür. Bazı platformlarda
        // Uri.TryCreate(..., UriKind.Absolute) bu değeri file URI gibi
        // yorumlayabildiği için root-relative path'i reddetmemeliyiz.
        //
        // Gerçek HTTP/HTTPS absolute URL gelirse de eski hostu taşımak yerine
        // yalnızca path/query/fragment kısmını alıp canonical BaseUrl'e bağlarız.
        if (Uri.TryCreate(normalized, UriKind.Absolute, out Uri? absoluteUri) &&
            (absoluteUri.Scheme == Uri.UriSchemeHttp ||
             absoluteUri.Scheme == Uri.UriSchemeHttps))
        {
            normalized =
                absoluteUri.PathAndQuery +
                absoluteUri.Fragment;
        }

        // Scheme-relative URL (//example.com/path) dış host kaçırma riski
        // oluşturmasın.
        if (normalized.StartsWith("//", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Public URL oluştururken scheme-relative URL kullanılamaz.",
                nameof(relativePath));
        }

        return $"{BaseUrl}/{normalized.TrimStart('/')}";
    }
}

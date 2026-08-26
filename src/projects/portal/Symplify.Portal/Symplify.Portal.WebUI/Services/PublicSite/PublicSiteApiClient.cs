using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Symplify.Portal.WebUI.Models.PublicSite;
using Symplify.Portal.WebUI.Options;

namespace Symplify.Portal.WebUI.Services.PublicSite;

public sealed class PublicSiteApiClient : IPublicSiteApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly PublicApiOptions _options;
    private readonly ILogger<PublicSiteApiClient> _logger;

    public PublicSiteApiClient(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        IOptions<PublicApiOptions> options,
        ILogger<PublicSiteApiClient> logger)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;
        _logger = logger;
    }

    public Task<PublicSiteBootstrapResponse> GetBootstrapAsync(string culture, CancellationToken cancellationToken) =>
        GetRequiredAsync<PublicSiteBootstrapResponse>("api/v1/public-site/bootstrap", culture, cancellationToken);

    public Task<PublicHomeResponse> GetHomeAsync(string culture, CancellationToken cancellationToken) =>
        GetRequiredAsync<PublicHomeResponse>("api/v1/public-site/home", culture, cancellationToken);

    public Task<PublicBoardsResponse> GetBoardsAsync(string culture, CancellationToken cancellationToken) =>
        GetRequiredAsync<PublicBoardsResponse>("api/v1/public-site/boards", culture, cancellationToken);

    public Task<PublicSectionsResponse> GetSectionsAsync(string culture, CancellationToken cancellationToken) =>
        GetRequiredAsync<PublicSectionsResponse>("api/v1/public-site/sections", culture, cancellationToken);

    public Task<PublicSectionResponse?> GetSectionByBindingKeyAsync(string bindingKey, string culture, CancellationToken cancellationToken) =>
        GetOptionalAsync<PublicSectionResponse>($"api/v1/public-site/sections/{Uri.EscapeDataString(bindingKey)}", culture, cancellationToken);

    public Task<PublicDocumentsResponse> GetDocumentsAsync(string culture, CancellationToken cancellationToken) =>
        GetRequiredAsync<PublicDocumentsResponse>("api/v1/public-site/documents", culture, cancellationToken);

    public Task<PublicContactResponse> GetContactAsync(string culture, CancellationToken cancellationToken) =>
        GetRequiredAsync<PublicContactResponse>("api/v1/public-site/contact", culture, cancellationToken);

    public Task<PublicContentsResponse> GetContentsAsync(string culture, CancellationToken cancellationToken) =>
        GetRequiredAsync<PublicContentsResponse>("api/v1/public-site/contents", culture, cancellationToken);

    private async Task<T> GetRequiredAsync<T>(string relativePath, string culture, CancellationToken cancellationToken)
        where T : class, new()
    {
        T? response = await GetOptionalAsync<T>(relativePath, culture, cancellationToken);
        return response ?? new T();
    }

    private async Task<T?> GetOptionalAsync<T>(string relativePath, string culture, CancellationToken cancellationToken)
        where T : class
    {
        string requestUri = BuildRequestUri(relativePath, culture);

        using HttpRequestMessage request = new(HttpMethod.Get, requestUri);
        AttachPublicApiHeaders(request, culture);

        using HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
        {
            string responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Public API request failed. Path: {Path}, Culture: {Culture}, StatusCode: {StatusCode}, Response: {Response}",
                relativePath,
                culture,
                (int)response.StatusCode,
                responseText);

            throw new PublicSiteApiException((int)response.StatusCode, "Public API isteği başarısız oldu.");
        }

        T? content = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
        return content;
    }

    private static string BuildRequestUri(string relativePath, string culture)
    {
        // Geriye uyumluluk için culture query string korunur.
        // API tarafında öncelik Portal'ın gönderdiği X-Culture header'ındadır.
        string separator = relativePath.Contains('?') ? "&" : "?";
        return $"{relativePath}{separator}culture={Uri.EscapeDataString(culture)}";
    }


    private string? ResolvePublicHost()
    {
        string? configuredHost = !string.IsNullOrWhiteSpace(_options.PublicHost)
            ? _options.PublicHost.Trim()
            : _httpContextAccessor.HttpContext?.Request.Host.Host;

        if (string.IsNullOrWhiteSpace(configuredHost))
        {
            return null;
        }

        if (Uri.TryCreate(configuredHost, UriKind.Absolute, out Uri? absoluteUri))
        {
            return absoluteUri.Host;
        }

        return configuredHost
            .Replace("https://", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("http://", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
    }

    private void AttachPublicApiHeaders(HttpRequestMessage request, string culture)
    {
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            request.Headers.TryAddWithoutValidation("X-Api-Key", _options.ApiKey);

        string? publicHost = ResolvePublicHost();

        if (!string.IsNullOrWhiteSpace(publicHost))
            request.Headers.TryAddWithoutValidation("X-Public-Host", publicHost);

        if (!string.IsNullOrWhiteSpace(culture))
        {
            request.Headers.TryAddWithoutValidation("X-Culture", culture);
            request.Headers.TryAddWithoutValidation("X-Symplify-Culture", culture);
            request.Headers.AcceptLanguage.ParseAdd(culture);
        }
    }
}

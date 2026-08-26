using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Symplify.Api.Application.Features.PublicSite.Constants;
using Symplify.Api.Application.Features.PublicSite.Contexts;
using Symplify.BackOffice.Domain.Organization;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.Api.WebAPI.Middleware;

public sealed class PublicApiKeyAuthenticationMiddleware
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private const string PublicHostHeaderName = "X-Public-Host";

    private readonly RequestDelegate _next;
    private readonly ILogger<PublicApiKeyAuthenticationMiddleware> _logger;

    public PublicApiKeyAuthenticationMiddleware(
        RequestDelegate next,
        ILogger<PublicApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, BackOfficeDbContext dbContext)
    {
        if (!RequiresPublicApiKey(context.Request))
        {
            await _next(context);
            return;
        }

        string? plainTextApiKey = ReadHeader(context.Request, ApiKeyHeaderName);
        if (string.IsNullOrWhiteSpace(plainTextApiKey))
        {
            await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, "API key is required.");
            return;
        }

        plainTextApiKey = plainTextApiKey.Trim();

        string keyPrefix = plainTextApiKey[..Math.Min(24, plainTextApiKey.Length)];
        string keyHash = ComputeSha256Hex(plainTextApiKey);

        OrganizationApiKey? apiKey = await dbContext.OrganizationApiKeys
            .Include(entity => entity.Organization)
            .FirstOrDefaultAsync(entity => entity.KeyPrefix == keyPrefix && entity.DeletedDate == null && entity.Organization.DeletedDate == null, context.RequestAborted);

        if (apiKey is null || !FixedTimeEquals(apiKey.KeyHash, keyHash))
        {
            _logger.LogWarning("Public API request rejected because API key could not be validated. Path: {Path}", context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, "API key is invalid.");
            return;
        }

        if (!IsUsable(apiKey))
        {
            _logger.LogWarning("Public API request rejected because API key is inactive, revoked, expired or organization is inactive. ApiKeyId: {ApiKeyId}", apiKey.Id);
            await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, "API key is not active.");
            return;
        }

        IReadOnlyCollection<string> scopes = SplitValues(apiKey.Scopes);
        if (!scopes.Contains(PublicApiScopes.CongressRead, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Public API request rejected because API key does not include required scope. ApiKeyId: {ApiKeyId}, RequiredScope: {Scope}", apiKey.Id, PublicApiScopes.CongressRead);
            await WriteProblemAsync(context, StatusCodes.Status403Forbidden, "API key does not have required scope.");
            return;
        }

        string publicHost = ResolvePublicHost(context.Request);
        if (!IsHostAllowed(publicHost, apiKey))
        {
            _logger.LogWarning("Public API request rejected because host is not allowed. ApiKeyId: {ApiKeyId}, Host: {Host}", apiKey.Id, publicHost);
            await WriteProblemAsync(context, StatusCodes.Status403Forbidden, "Public host is not allowed for this API key.");
            return;
        }

        if (!IsIpAllowed(context.Connection.RemoteIpAddress, apiKey.AllowedIpAddresses))
        {
            _logger.LogWarning("Public API request rejected because remote IP is not allowed. ApiKeyId: {ApiKeyId}, RemoteIp: {RemoteIp}", apiKey.Id, context.Connection.RemoteIpAddress?.ToString());
            await WriteProblemAsync(context, StatusCodes.Status403Forbidden, "Remote IP is not allowed for this API key.");
            return;
        }

        apiKey.LastUsedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(context.RequestAborted);

        context.Items[PublicApiContext.HttpContextItemKey] = new PublicApiContext
        {
            ApiKeyId = apiKey.Id,
            OrganizationId = apiKey.OrganizationId,
            OrganizationCode = apiKey.Organization.Code,
            OrganizationName = apiKey.Organization.Name,
            KeyPrefix = apiKey.KeyPrefix,
            PublicHost = publicHost,
            Scopes = scopes
        };

        await _next(context);
    }

    private static bool RequiresPublicApiKey(HttpRequest request)
    {
        return request.Path.StartsWithSegments("/api/v1/public-site", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUsable(OrganizationApiKey apiKey)
    {
        DateTime now = DateTime.UtcNow;

        return apiKey.IsActive &&
               apiKey.RevokedAt is null &&
               (!apiKey.ExpiresAt.HasValue || apiKey.ExpiresAt.Value > now) &&
               apiKey.Organization.IsActive;
    }

    private static bool IsHostAllowed(string publicHost, OrganizationApiKey apiKey)
    {
        string normalizedHost = NormalizeHost(publicHost);

        IReadOnlyCollection<string> allowedDomains = SplitValues(apiKey.AllowedDomains);
        if (allowedDomains.Count > 0)
            return allowedDomains.Any(domain => IsDomainMatch(normalizedHost, domain));

        string organizationHost = NormalizeHost(apiKey.Organization.HostUrl);
        string organizationWebsite = NormalizeHost(apiKey.Organization.WebsiteUrl);

        if (!string.IsNullOrWhiteSpace(organizationHost) || !string.IsNullOrWhiteSpace(organizationWebsite))
        {
            return IsSameHost(normalizedHost, organizationHost) ||
                   IsSameHost(normalizedHost, organizationWebsite);
        }

        // Development fallback. In production, prefer filling AllowedDomains or Organization HostUrl.
        return true;
    }

    private static bool IsDomainMatch(string actualHost, string allowedDomain)
    {
        string normalizedAllowedDomain = NormalizeHost(allowedDomain);

        if (string.Equals(normalizedAllowedDomain, "*", StringComparison.Ordinal))
            return true;

        return IsSameHost(actualHost, normalizedAllowedDomain);
    }

    private static bool IsSameHost(string first, string second)
    {
        if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second))
            return false;

        return string.Equals(NormalizeWww(first), NormalizeWww(second), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeWww(string host)
    {
        return host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? host[4..] : host;
    }

    private static bool IsIpAllowed(IPAddress? remoteIpAddress, string? allowedIpAddresses)
    {
        IReadOnlyCollection<string> allowedIps = SplitValues(allowedIpAddresses);
        if (allowedIps.Count == 0)
            return true;

        string? remoteIp = remoteIpAddress?.ToString();
        if (string.IsNullOrWhiteSpace(remoteIp))
            return false;

        return allowedIps.Contains(remoteIp, StringComparer.OrdinalIgnoreCase);
    }

    private static string ResolvePublicHost(HttpRequest request)
    {
        string? publicHost = ReadHeader(request, PublicHostHeaderName);

        if (!string.IsNullOrWhiteSpace(publicHost))
            return publicHost;

        string? forwardedHost = ReadHeader(request, "X-Forwarded-Host");
        if (!string.IsNullOrWhiteSpace(forwardedHost))
            return forwardedHost.Split(',')[0].Trim();

        return request.Host.Value;
    }

    private static string? ReadHeader(HttpRequest request, string headerName)
    {
        return request.Headers.TryGetValue(headerName, out var value) ? value.FirstOrDefault()?.Trim() : null;
    }

    private static IReadOnlyCollection<string> SplitValues(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Array.Empty<string>();

        return value
            .Split(new[] { ',', ';', '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string NormalizeHost(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string normalized = value.Trim().TrimEnd('/');

        if (normalized.Contains(',', StringComparison.Ordinal))
            normalized = normalized.Split(',')[0].Trim();

        if (!normalized.Contains("://", StringComparison.Ordinal))
            normalized = $"https://{normalized}";

        if (Uri.TryCreate(normalized, UriKind.Absolute, out Uri? uri))
            return uri.Host.ToLowerInvariant();

        return value.Trim().Split('/')[0].Split(':')[0].ToLowerInvariant();
    }

    private static string ComputeSha256Hex(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private static bool FixedTimeEquals(string storedHash, string candidateHash)
    {
        if (string.IsNullOrWhiteSpace(storedHash) || string.IsNullOrWhiteSpace(candidateHash))
            return false;

        try
        {
            byte[] storedBytes = Convert.FromHexString(storedHash.Trim());
            byte[] candidateBytes = Convert.FromHexString(candidateHash.Trim());

            return storedBytes.Length == candidateBytes.Length &&
                   CryptographicOperations.FixedTimeEquals(storedBytes, candidateBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new
        {
            status = statusCode,
            title = message
        });
    }
}

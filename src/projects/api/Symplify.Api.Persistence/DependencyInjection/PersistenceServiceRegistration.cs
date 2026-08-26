using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Symplify.Api.Application.Services.PublicSite;
using Symplify.Api.Persistence.PublicSite;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.Api.Persistence.DependencyInjection;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddSymplifyApiPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection configuration is required for Symplify.Api.");

        services.AddDbContext<BackOfficeDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.Configure<PublicAssetOptions>(options =>
        {
            BindPublicAssetOptions(configuration, options);
            BindObjectStorageOptions(configuration, options);
            NormalizePublicAssetOptions(options);
        });

        services.AddSingleton<IPublicAssetUrlBuilder, PublicAssetUrlBuilder>();
        services.AddScoped<IPublicSiteReadRepository, PublicSiteReadRepository>();

        return services;
    }

    private static void BindPublicAssetOptions(IConfiguration configuration, PublicAssetOptions options)
    {
        string sectionName = PublicAssetOptions.SectionName;

        options.BaseUrl = configuration[$"{sectionName}:BaseUrl"];
        options.RelativePathPrefix = configuration[$"{sectionName}:RelativePathPrefix"];
        options.StaticFilesBaseUrl = configuration[$"{sectionName}:StaticFilesBaseUrl"];
        options.ObjectStorageBaseUrl = configuration[$"{sectionName}:ObjectStorageBaseUrl"];
        options.ImagesObjectStorageBaseUrl = configuration[$"{sectionName}:ImagesObjectStorageBaseUrl"];
        options.DocumentsObjectStorageBaseUrl = configuration[$"{sectionName}:DocumentsObjectStorageBaseUrl"];
        options.UpstreamPublicAssetsBaseUrl = configuration[$"{sectionName}:UpstreamPublicAssetsBaseUrl"];

        if (bool.TryParse(configuration[$"{sectionName}:AllowInvalidUpstreamCertificate"], out bool allowInvalidCertificateValue))
            options.AllowInvalidUpstreamCertificate = allowInvalidCertificateValue;

        if (bool.TryParse(configuration[$"{sectionName}:PreferDirectObjectStorageForAssets"], out bool preferDirectObjectStorageValue))
            options.PreferDirectObjectStorageForAssets = preferDirectObjectStorageValue;

        // Backward compatibility with the previous temporary DirectMinio* configuration.
        string? legacyEndpoint = configuration[$"{sectionName}:DirectMinioEndpoint"];
        if (!string.IsNullOrWhiteSpace(legacyEndpoint))
            options.Endpoint = legacyEndpoint;

        string? legacyAccessKey = configuration[$"{sectionName}:DirectMinioAccessKey"];
        if (!string.IsNullOrWhiteSpace(legacyAccessKey))
            options.AccessKey = legacyAccessKey;

        string? legacySecretKey = configuration[$"{sectionName}:DirectMinioSecretKey"];
        if (!string.IsNullOrWhiteSpace(legacySecretKey))
            options.SecretKey = legacySecretKey;

        string? legacyRegion = configuration[$"{sectionName}:DirectMinioRegion"];
        if (!string.IsNullOrWhiteSpace(legacyRegion))
            options.Region = legacyRegion.Trim();

        if (bool.TryParse(configuration[$"{sectionName}:PreferDirectMinioForAssets"], out bool legacyPreferDirectMinioValue))
            options.PreferDirectObjectStorageForAssets = legacyPreferDirectMinioValue;
    }

    private static void BindObjectStorageOptions(IConfiguration configuration, PublicAssetOptions options)
    {
        string sectionName = PublicAssetOptions.ObjectStorageSectionName;

        string? provider = configuration[$"{sectionName}:Provider"];
        if (!string.IsNullOrWhiteSpace(provider))
            options.Provider = provider.Trim();

        string? endpoint = configuration[$"{sectionName}:Endpoint"];
        if (!string.IsNullOrWhiteSpace(endpoint))
            options.Endpoint = endpoint.Trim();

        if (bool.TryParse(configuration[$"{sectionName}:UseSsl"], out bool useSslValue))
            options.UseSsl = useSslValue;

        string? accessKey = configuration[$"{sectionName}:AccessKey"];
        if (!string.IsNullOrWhiteSpace(accessKey))
            options.AccessKey = accessKey;

        string? secretKey = configuration[$"{sectionName}:SecretKey"];
        if (!string.IsNullOrWhiteSpace(secretKey))
            options.SecretKey = secretKey;

        string? region = configuration[$"{sectionName}:Region"];
        if (!string.IsNullOrWhiteSpace(region))
            options.Region = region.Trim();

        string? congressImagesBucket = configuration[$"{sectionName}:Buckets:CongressImages"];
        if (!string.IsNullOrWhiteSpace(congressImagesBucket))
            options.CongressImagesBucket = congressImagesBucket.Trim();

        string? congressDocumentsBucket = configuration[$"{sectionName}:Buckets:CongressDocuments"];
        if (!string.IsNullOrWhiteSpace(congressDocumentsBucket))
            options.CongressDocumentsBucket = congressDocumentsBucket.Trim();

        string? submissionsBucket = configuration[$"{sectionName}:Buckets:Submissions"];
        if (!string.IsNullOrWhiteSpace(submissionsBucket))
            options.SubmissionsBucket = submissionsBucket.Trim();
    }

    private static void NormalizePublicAssetOptions(PublicAssetOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
            options.BaseUrl = "http://localhost:5200";

        if (string.IsNullOrWhiteSpace(options.ObjectStorageBaseUrl))
            options.ObjectStorageBaseUrl = CombineUrl(options.BaseUrl, "public-assets");

        if (string.IsNullOrWhiteSpace(options.ImagesObjectStorageBaseUrl))
            options.ImagesObjectStorageBaseUrl = options.ObjectStorageBaseUrl;

        if (string.IsNullOrWhiteSpace(options.DocumentsObjectStorageBaseUrl))
            options.DocumentsObjectStorageBaseUrl = options.ObjectStorageBaseUrl;

        options.AllowedPublicBuckets.Clear();
        AddIfNotEmpty(options.AllowedPublicBuckets, options.CongressImagesBucket);
        AddIfNotEmpty(options.AllowedPublicBuckets, options.CongressDocumentsBucket);
    }

    private static void AddIfNotEmpty(ISet<string> target, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            target.Add(value.Trim());
    }

    private static string CombineUrl(params string?[] segments)
    {
        string[] cleanedSegments = segments
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .Select((segment, index) => index == 0 ? segment!.Trim().TrimEnd('/') : segment!.Trim().Trim('/'))
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .ToArray();

        return string.Join('/', cleanedSegments);
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.Application.Storage;

public static class ObjectStorageServiceRegistration
{
    /// <summary>
    /// Registers only ObjectStorage options. This method does not register a concrete storage provider.
    /// Use this when an application wants to bind configuration but does not want to enable storage yet.
    /// </summary>
    public static IServiceCollection AddObjectStorageCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<ObjectStorageOptions>()
            .Bind(configuration.GetSection(ObjectStorageOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Provider),
                "ObjectStorage:Provider is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Endpoint),
                "ObjectStorage:Endpoint is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.AccessKey),
                "ObjectStorage:AccessKey is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.SecretKey),
                "ObjectStorage:SecretKey is required.");

        return services;
    }

    /// <summary>
    /// Registers ObjectStorage options and maps IObjectStorageService to the supplied implementation.
    /// Core.Application does not know MinIO/S3/Azure implementations; the application passes the implementation type.
    /// </summary>
    public static IServiceCollection AddObjectStorage<TImplementation>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TImplementation : class, IObjectStorageService
    {
        services.AddObjectStorageCore(configuration);
        services.TryAddScoped<IObjectStorageService, TImplementation>();

        return services;
    }
}

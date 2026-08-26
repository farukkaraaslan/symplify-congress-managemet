using Core.Application.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Minio;
using Symplify.BackOffice.Application.Services.Authentication;
using Symplify.BackOffice.Application.Services.Email;
using Symplify.BackOffice.Application.Services.Mailing;
using Symplify.BackOffice.Application.Services.RoleAdministration;
using Symplify.BackOffice.Application.Services.Storage;
using Symplify.BackOffice.Application.Services.UserAdministration;
using Symplify.BackOffice.Infrastructure.Identity;
using Symplify.BackOffice.Infrastructure.Email;
using Symplify.BackOffice.Infrastructure.Email.Ses;
using Symplify.BackOffice.Infrastructure.Storage;
using Symplify.BackOffice.Application.Services.Urls;
using Symplify.BackOffice.Infrastructure.Urls;
using Symplify.BackOffice.Infrastructure.ParticipationCertificates;

namespace Symplify.BackOffice.Infrastructure.DependencyInjection;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddBackOfficeInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BackOfficeMailOptions>(configuration.GetSection(BackOfficeMailOptions.SectionName));
        services.Configure<MailCredentialProtectionOptions>(configuration.GetSection(MailCredentialProtectionOptions.SectionName));
        services.Configure<MailTemplateOptions>(configuration.GetSection(MailTemplateOptions.SectionName));

        services.AddSingleton<IPublicUrlService, PublicUrlService>();
        services.AddSingleton<IMailCredentialProtector, AesGcmMailCredentialProtector>();
        services.AddScoped<IAmazonSesSnsAdapter, AmazonSesSnsAdapter>();
        services.AddScoped<IBackOfficeEmailSender, SmtpBackOfficeEmailSender>();
        services.AddHostedService<MailOutboxDispatcherHostedService>();
        services.AddHostedService<ParticipationCertificateGenerationHostedService>();

        services.AddScoped<IBackOfficeIdentityService, BackOfficeIdentityService>();
        services.AddScoped<IPasswordGenerator, SecurePasswordGenerator>();
        services.AddScoped<IUserAdministrationService, BackOfficeUserAdministrationService>();
        services.AddScoped<IRoleAdministrationService, BackOfficeRoleAdministrationService>();

        return services;
    }

    public static IServiceCollection AddBackOfficeMinioObjectStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        ObjectStorageOptions options = configuration
            .GetSection(ObjectStorageOptions.SectionName)
            .Get<ObjectStorageOptions>()
            ?? new ObjectStorageOptions();

        string endpoint = NormalizeEndpoint(options.Endpoint);
        string accessKey = options.AccessKey?.Trim() ?? string.Empty;
        string secretKey = options.SecretKey ?? string.Empty;
        bool useSsl = options.UseSsl;

        ValidateObjectStorageOptions(endpoint, accessKey, secretKey);

        services.AddObjectStorageCore(configuration);

        services.RemoveAll<IMinioClient>();
        services.AddSingleton<IMinioClient>(_ =>
        {
            IMinioClient client = new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey);

            if (useSsl)
            {
                client = client.WithSSL();
            }

            return client.Build();
        });

        services.RemoveAll<IObjectStorageService>();
        services.RemoveAll<IObjectStoragePrefixCleanupService>();
        services.AddScoped<MinioObjectStorageService>();
        services.AddScoped<IObjectStorageService>(provider => provider.GetRequiredService<MinioObjectStorageService>());
        services.AddScoped<IObjectStoragePrefixCleanupService>(provider => provider.GetRequiredService<MinioObjectStorageService>());
        services.AddScoped<IObjectStorageRangeReader, MinioObjectStorageRangeReader>();

        return services;
    }

    private static string NormalizeEndpoint(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            return string.Empty;

        string normalizedEndpoint = endpoint.Trim().TrimEnd('/');

        if (normalizedEndpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            return normalizedEndpoint["http://".Length..];

        if (normalizedEndpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return normalizedEndpoint["https://".Length..];

        return normalizedEndpoint;
    }

    private static void ValidateObjectStorageOptions(
        string endpoint,
        string accessKey,
        string secretKey)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new InvalidOperationException("ObjectStorage:Endpoint is required.");

        if (string.IsNullOrWhiteSpace(accessKey))
            throw new InvalidOperationException("ObjectStorage:AccessKey is required.");

        if (string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException("ObjectStorage:SecretKey is required.");
    }
}
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Core.CrossCuttingConcerns.Localization;

public static class ServiceCollectionResourceLocalizationManagerExtension
{
    public static IServiceCollection AddYamlResourceLocalization(this IServiceCollection services)
    {
        services.AddScoped<ILocalizationService, ResourceLocalizationManager>(_ =>
        {
            Dictionary<string, Dictionary<string, string>> resources = GetLocalizationResources();

            return new ResourceLocalizationManager(resources);
        });

        return services;
    }

    private static Dictionary<string, Dictionary<string, string>> GetLocalizationResources()
    {
        Dictionary<string, Dictionary<string, string>> resources = [];

        string featureDirRoot = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Features");

        if (!Directory.Exists(featureDirRoot))
            return resources;

        string[] featureDirs = Directory.GetDirectories(featureDirRoot);

        foreach (string featureDir in featureDirs)
        {
            string featureName = Path.GetFileName(featureDir);
            string localeDir = Path.Combine(featureDir, "Resources", "Locales");

            if (Directory.Exists(localeDir))
            {
                ProcessLocaleFiles(localeDir, featureName, resources);
            }
        }

        return resources;
    }

    private static void ProcessLocaleFiles(string localeDir, string featureName, Dictionary<string, Dictionary<string, string>> resources)
    {
        string[] localeFiles = Directory.GetFiles(localeDir);

        foreach (string localeFile in localeFiles)
        {
            string localeName = Path.GetFileNameWithoutExtension(localeFile);
            int separatorIndex = localeName.IndexOf('.');

            if (separatorIndex <= 0 || !File.Exists(localeFile)) continue;

            string localeCulture = localeName[(separatorIndex + 1)..];

            if (!resources.ContainsKey(localeCulture))
                resources.Add(localeCulture, []);

            resources[localeCulture].Add(featureName, localeFile);
        }
    }
}
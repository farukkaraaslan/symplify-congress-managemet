using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Symplify.BackOffice.Domain.Localization;
using Symplify.BackOffice.Persistence.Contexts;
using Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;

namespace Symplify.BackOffice.Persistence.Seeding.Seeders;

public sealed class LocalizationResourceSeeder
{
    private readonly BackOfficeDbContext _context;
    private readonly ILogger<LocalizationResourceSeeder> _logger;

    public LocalizationResourceSeeder(
        BackOfficeDbContext context,
        ILogger<LocalizationResourceSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        Language? turkishLanguage = await _context.Languages
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(language => language.Culture == "tr-TR", cancellationToken);

        Language? englishLanguage = await _context.Languages
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(language => language.Culture == "en-US", cancellationToken);

        if (turkishLanguage is null || englishLanguage is null)
            throw new InvalidOperationException("Localization resource seed requires tr-TR and en-US languages.");

        IReadOnlyCollection<ResourceSeedDefinition> resources = GetResources();
        DateTime utcNow = DateTime.UtcNow;

        foreach (ResourceSeedDefinition resource in resources)
        {
            ResourceKey? resourceKey = await _context.ResourceKeys
                .Include(key => key.Values)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    key => key.KeyName == resource.KeyName,
                    cancellationToken);

            if (resourceKey is null)
            {
                resourceKey = new ResourceKey
                {
                    Id = Guid.NewGuid(),
                    AreaName = resource.AreaName,
                    KeyName = resource.KeyName,
                    CreatedDate = utcNow,
                    CreatedBy = "System",
                    Values = new List<ResourceValue>()
                };

                await _context.ResourceKeys.AddAsync(resourceKey, cancellationToken);
                _logger.LogInformation("Resource key seed added: {KeyName}", resource.KeyName);
            }
            else
            {
                resourceKey.AreaName = resource.AreaName;
            }

            UpsertResourceValue(resourceKey, turkishLanguage.Id, resource.TurkishValue, utcNow);
            UpsertResourceValue(resourceKey, englishLanguage.Id, resource.EnglishValue, utcNow);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyCollection<ResourceSeedDefinition> GetResources()
    {
        return CommonResourceSeedDefinitions.All
            .Concat(DataTableResourceSeedDefinitions.All)
            .Concat(SidebarResourceSeedDefinitions.All)
            .Concat(BackOfficeLookupResourceSeedDefinitions.All)
            .Concat(BackOfficeTopicResourceSeedDefinitions.All)
            .GroupBy(resource => resource.KeyName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private static void UpsertResourceValue(
        ResourceKey resourceKey,
        Guid languageId,
        string value,
        DateTime utcNow)
    {
        ResourceValue? resourceValue = resourceKey.Values
            .FirstOrDefault(item => item.LanguageId == languageId);

        if (resourceValue is null)
        {
            resourceKey.Values.Add(new ResourceValue
            {
                Id = Guid.NewGuid(),
                LanguageId = languageId,
                Value = value,
                CreatedDate = utcNow,
                CreatedBy = "System"
            });

            return;
        }

        // Seed temel sistem metinlerini güncel tutar. Admin panelden değişikliklerin ezilmesini istemiyorsan
        // buraya config flag ekleyip production'da overwrite kapatabilirsin.
        if (string.Equals(resourceValue.Value, value, StringComparison.Ordinal))
            return;

        resourceValue.Value = value;
        resourceValue.UpdatedDate = utcNow;
        resourceValue.UpdatedBy = "System";
    }
}

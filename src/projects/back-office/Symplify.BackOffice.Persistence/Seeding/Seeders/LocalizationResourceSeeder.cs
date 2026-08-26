using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Domain.Localization;
using Symplify.BackOffice.Persistence.Contexts;
using Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;
using Symplify.BackOffice.Persistence.Seeding.Options;

namespace Symplify.BackOffice.Persistence.Seeding.Seeders;

public sealed class LocalizationResourceSeeder
{
    private static readonly string[] RequiredCultures = ["tr-TR", "en-US"];

    private readonly BackOfficeDbContext _context;
    private readonly LocalizationSeedOptions _options;
    private readonly ILogger<LocalizationResourceSeeder> _logger;

    public LocalizationResourceSeeder(
        BackOfficeDbContext context,
        IOptions<LocalizationSeedOptions> options,
        ILogger<LocalizationResourceSeeder> logger)
    {
        _context = context;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Localization seed is disabled (LocalizationSeed:Enabled = false). Skipping.");
            return;
        }

        // This validation deliberately runs before the first database operation.
        // Duplicate or incomplete source definitions must never be hidden by deduplication.
        IReadOnlyCollection<ResourceSeedDefinition> resources = GetResources();

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            List<Language> allLanguages = await _context.Languages
                .IgnoreQueryFilters()
                .ToListAsync(cancellationToken);

            List<Language> requiredLanguages = allLanguages
                .Where(language => RequiredCultures.Any(requiredCulture =>
                    string.Equals(language.Culture, requiredCulture, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            EnsureRequiredLanguagesAreUnambiguous(requiredLanguages);

            Language turkishLanguage = requiredLanguages.Single(language =>
                string.Equals(language.Culture, "tr-TR", StringComparison.OrdinalIgnoreCase));
            Language englishLanguage = requiredLanguages.Single(language =>
                string.Equals(language.Culture, "en-US", StringComparison.OrdinalIgnoreCase));

            // Load the localization key space once so case-insensitive DB conflicts cannot
            // escape an exact-match query and produce a second key during insert.
            List<ResourceKey> existingResourceKeys = await _context.ResourceKeys
                .Include(key => key.Values)
                .IgnoreQueryFilters()
                .ToListAsync(cancellationToken);

            EnsureDatabaseResourceKeysAreUnambiguous(existingResourceKeys, resources);
            EnsureDatabaseResourceValuesAreUnambiguous(existingResourceKeys);

            Dictionary<string, ResourceKey> resourceKeysByName = existingResourceKeys
                .ToDictionary(key => key.KeyName, StringComparer.Ordinal);

            DateTime utcNow = DateTime.UtcNow;
            int keysAdded = 0;
            int translationsAdded = 0;
            int translationsUpdated = 0;

            _logger.LogInformation(
                "Localization resource seed started. Total definitions: {Count}. OverwriteExistingTranslations: {Overwrite}",
                resources.Count,
                _options.OverwriteExistingTranslations);

            foreach (ResourceSeedDefinition resource in resources)
            {
                if (!resourceKeysByName.TryGetValue(resource.KeyName, out ResourceKey? resourceKey))
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
                    resourceKeysByName.Add(resource.KeyName, resourceKey);
                    keysAdded++;

                    _logger.LogDebug("Localization key added: {KeyName}", resource.KeyName);
                }
                else
                {
                    resourceKey.AreaName = resource.AreaName;
                    resourceKey.DeletedDate = null;
                    resourceKey.DeletedBy = null;
                }

                UpsertResourceValue(
                    resourceKey,
                    turkishLanguage.Id,
                    "tr-TR",
                    resource.TurkishValue,
                    utcNow,
                    _options.OverwriteExistingTranslations,
                    ref translationsAdded,
                    ref translationsUpdated);

                UpsertResourceValue(
                    resourceKey,
                    englishLanguage.Id,
                    "en-US",
                    resource.EnglishValue,
                    utcNow,
                    _options.OverwriteExistingTranslations,
                    ref translationsAdded,
                    ref translationsUpdated);
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Localization resource seed completed. Keys added: {KeysAdded} | Translations added: {TranslationsAdded} | Translations updated: {TranslationsUpdated}",
                keysAdded,
                translationsAdded,
                translationsUpdated);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    /// <summary>
    /// Returns the complete validated list of all seed definitions.
    /// No duplicate is silently selected or removed.
    /// </summary>
    public static IReadOnlyCollection<ResourceSeedDefinition> GetResources()
    {
        LocalizationSeedDefinitionAuditResult auditResult = ValidateSeedDefinitions(GetAllResources());
        ThrowIfInvalid(auditResult);
        return auditResult.Resources;
    }

    public static IReadOnlyCollection<ResourceSeedDefinition> GetAllResources()
    {
        return CommonResourceSeedDefinitions.All
            .Concat(DataTableResourceSeedDefinitions.All)
            .Concat(SidebarResourceSeedDefinitions.All)
            .Concat(AuthResourceSeedDefinitions.All)
            .Concat(PortalResourceSeedDefinitions.All)
            .Concat(MailResourceSeedDefinitions.All)
            .Concat(BulkEmailResourceSeedDefinitions.All)
            .Concat(MailDeliveryResourceSeedDefinitions.All)
            .Concat(AcceptanceLetterResourceSeedDefinitions.All)
            .Concat(OrganizationValidationResourceSeedDefinitions.All)
            .Concat(OrganizationResourceSeedDefinitions.All)
            .Concat(CongressResourceSeedDefinitions.All)
            .Concat(CongressManageResourceSeedDefinitions.All)
            .Concat(CongressRichTextResourceSeedDefinitions.All)
            .Concat(CongressManagementLocalizationStandardResourceSeedDefinitions.All)
            .Concat(CongressSliderResourceSeedDefinitions.All)
            .Concat(CongressSectionResourceSeedDefinitions.All)
            .Concat(CongressAnnouncementResourceSeedDefinitions.All)
            .Concat(CongressImportantDateResourceSeedDefinitions.All)
            .Concat(CongressDocumentResourceSeedDefinitions.All)
            .Concat(CongressPaymentPlanResourceSeedDefinitions.All)
            .Concat(CongressBoardMemberResourceSeedDefinitions.All)
            .Concat(CongressWorkflowManagementResourceSeedDefinitions.All)
            .Concat(BackOfficeLookupResourceSeedDefinitions.All)
            .Concat(BackOfficeTopicResourceSeedDefinitions.All)
            .Concat(BackOfficeWorkflowResourceSeedDefinitions.All)
            .Concat(BackOfficeUiStandardizationResourceSeedDefinitions.All)
            .Concat(TransactionStatusValidationResourceSeedDefinitions.All)
            .Concat(ClientLocalizationResourceSeedDefinitions.All)
            .Concat(SubmissionUiResourceSeedDefinitions.All)
            .Concat(SubmissionResourceSeedDefinitions.All)
            .Concat(SubmissionManagementUiResourceSeedDefinitions.All)
            .Concat(ProgramManagementResourceSeedDefinitions.All)
            .Concat(AbstractBookResourceSeedDefinitions.All)
            .Concat(SubmissionFinalFileShortLinkResourceSeedDefinitions.All)
            .Concat(SubmissionFinalFilesResourceSeedDefinitions.All)
            .Concat(ReviewerEvaluationResourceSeedDefinitions.All)
            .Concat(UserManagementResourceSeedDefinitions.All)
            .Concat(HomePageResourceSeedDefinitions.All)
            .Concat(ProfileResourceSeedDefinitions.All)
            .Concat(ReleaseLocalizationResourceSeedDefinitions.All)
            .Concat(ContentAssetResourceSeedDefinitions.All)
            .Concat(HardcodedStringsLocalizationSeedDefinitions.All)
            .ToList();
    }

    public static LocalizationSeedDefinitionAuditResult ValidateSeedDefinitions(
        IReadOnlyCollection<ResourceSeedDefinition> resources)
    {
        ArgumentNullException.ThrowIfNull(resources);

        List<IGrouping<string, ResourceSeedDefinition>> duplicateGroups = resources
            .Where(resource => !string.IsNullOrEmpty(resource.KeyName))
            .GroupBy(resource => resource.KeyName, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .ToList();

        List<string> valueConflictDuplicateKeys = duplicateGroups
            .Where(group => group
                .Select(resource => new
                {
                    resource.TurkishValue,
                    resource.EnglishValue
                })
                .Distinct()
                .Count() > 1)
            .Select(group => group.Key)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        List<string> areaConflictDuplicateKeys = duplicateGroups
            .Where(group => !valueConflictDuplicateKeys.Contains(group.Key, StringComparer.Ordinal))
            .Where(group => group
                .Select(resource => resource.AreaName)
                .Distinct(StringComparer.Ordinal)
                .Count() > 1)
            .Select(group => group.Key)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        List<string> exactDuplicateKeys = duplicateGroups
            .Where(group => !valueConflictDuplicateKeys.Contains(group.Key, StringComparer.Ordinal))
            .Where(group => !areaConflictDuplicateKeys.Contains(group.Key, StringComparer.Ordinal))
            .Select(group => group.Key)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        List<IReadOnlyCollection<string>> caseInsensitiveDuplicateKeyGroups = resources
            .Where(resource => !string.IsNullOrEmpty(resource.KeyName))
            .GroupBy(resource => resource.KeyName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .Select(resource => resource.KeyName)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(key => key, StringComparer.Ordinal)
                .ToList())
            .Where(keys => keys.Count > 1)
            .Select(keys => (IReadOnlyCollection<string>)keys)
            .ToList();

        List<string> emptyKeyDefinitions = resources
            .Where(resource => string.IsNullOrWhiteSpace(resource.KeyName))
            .Select((_, index) => $"definition[{index}]")
            .ToList();

        List<string> keysWithLeadingOrTrailingWhitespace = resources
            .Where(resource => !string.IsNullOrEmpty(resource.KeyName))
            .Where(resource => !string.Equals(resource.KeyName, resource.KeyName.Trim(), StringComparison.Ordinal))
            .Select(resource => resource.KeyName)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        List<string> keysWithEmptyArea = resources
            .Where(resource => string.IsNullOrWhiteSpace(resource.AreaName))
            .Select(resource => DisplayKey(resource.KeyName))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        List<string> keysWithEmptyTurkishValue = resources
            .Where(resource => string.IsNullOrWhiteSpace(resource.TurkishValue))
            .Select(resource => DisplayKey(resource.KeyName))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        List<string> keysWithEmptyEnglishValue = resources
            .Where(resource => string.IsNullOrWhiteSpace(resource.EnglishValue))
            .Select(resource => DisplayKey(resource.KeyName))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        List<ResourceSeedDefinition> orderedResources = resources
            .OrderBy(resource => resource.KeyName, StringComparer.Ordinal)
            .ThenBy(resource => resource.AreaName, StringComparer.Ordinal)
            .ToList();

        return new LocalizationSeedDefinitionAuditResult(
            orderedResources,
            exactDuplicateKeys,
            valueConflictDuplicateKeys,
            areaConflictDuplicateKeys,
            caseInsensitiveDuplicateKeyGroups,
            emptyKeyDefinitions,
            keysWithLeadingOrTrailingWhitespace,
            keysWithEmptyArea,
            keysWithEmptyTurkishValue,
            keysWithEmptyEnglishValue);
    }

    public static void ThrowIfInvalid(LocalizationSeedDefinitionAuditResult auditResult)
    {
        ArgumentNullException.ThrowIfNull(auditResult);

        if (auditResult.IsValid)
            return;

        List<string> issues = [];
        AddIssue(issues, "exact duplicate", auditResult.ExactDuplicateKeys);
        AddIssue(issues, "value conflict", auditResult.ValueConflictDuplicateKeys);
        AddIssue(issues, "area conflict", auditResult.AreaConflictDuplicateKeys);
        AddIssue(
            issues,
            "case-insensitive conflict",
            auditResult.CaseInsensitiveDuplicateKeyGroups.Select(group => string.Join(" / ", group)).ToList());
        AddIssue(issues, "empty key", auditResult.EmptyKeyDefinitions);
        AddIssue(issues, "key whitespace", auditResult.KeysWithLeadingOrTrailingWhitespace);
        AddIssue(issues, "empty area", auditResult.KeysWithEmptyArea);
        AddIssue(issues, "empty tr-TR", auditResult.KeysWithEmptyTurkishValue);
        AddIssue(issues, "empty en-US", auditResult.KeysWithEmptyEnglishValue);

        throw new InvalidOperationException(
            "Localization seed definitions are invalid. Database seeding was not started. " +
            string.Join(" | ", issues));
    }

    private static void EnsureRequiredLanguagesAreUnambiguous(IReadOnlyCollection<Language> languages)
    {
        foreach (string culture in RequiredCultures)
        {
            List<Language> matches = languages
                .Where(language => string.Equals(language.Culture, culture, StringComparison.Ordinal))
                .ToList();

            if (matches.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Localization resource seed requires exactly one '{culture}' language, but none was found.");
            }

            if (matches.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Localization resource seed requires exactly one '{culture}' language, but {matches.Count} rows were found.");
            }
        }

        List<IGrouping<string, Language>> caseInsensitiveDuplicates = languages
            .GroupBy(language => language.Culture, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .ToList();

        if (caseInsensitiveDuplicates.Count > 0)
        {
            throw new InvalidOperationException(
                "Localization language rows contain case-insensitive duplicates: " +
                string.Join(", ", caseInsensitiveDuplicates.Select(group => group.Key)));
        }
    }

    private static void EnsureDatabaseResourceKeysAreUnambiguous(
        IReadOnlyCollection<ResourceKey> resourceKeys,
        IReadOnlyCollection<ResourceSeedDefinition> seedDefinitions)
    {
        List<string> exactDuplicates = resourceKeys
            .GroupBy(key => key.KeyName, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        if (exactDuplicates.Count > 0)
        {
            throw new InvalidOperationException(
                "Database contains duplicate ResourceKey rows. Resolve them before seeding: " +
                string.Join(", ", exactDuplicates.Take(20)));
        }

        List<IReadOnlyCollection<string>> caseInsensitiveDuplicates = resourceKeys
            .GroupBy(key => key.KeyName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .Select(key => key.KeyName)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(key => key, StringComparer.Ordinal)
                .ToList())
            .Where(group => group.Count > 1)
            .Select(group => (IReadOnlyCollection<string>)group)
            .ToList();

        if (caseInsensitiveDuplicates.Count > 0)
        {
            throw new InvalidOperationException(
                "Database contains case-insensitive ResourceKey conflicts. Resolve them before seeding: " +
                string.Join(" | ", caseInsensitiveDuplicates.Take(20).Select(group => string.Join(", ", group))));
        }

        Dictionary<string, string> canonicalSeedKeyByNormalizedName = seedDefinitions
            .ToDictionary(
                seed => seed.KeyName.ToUpperInvariant(),
                seed => seed.KeyName,
                StringComparer.Ordinal);

        List<string> caseMismatches = resourceKeys
            .Where(resourceKey => canonicalSeedKeyByNormalizedName.TryGetValue(
                resourceKey.KeyName.ToUpperInvariant(),
                out string? canonicalKey) &&
                !string.Equals(resourceKey.KeyName, canonicalKey, StringComparison.Ordinal))
            .Select(resourceKey => $"{resourceKey.KeyName} -> {canonicalSeedKeyByNormalizedName[resourceKey.KeyName.ToUpperInvariant()]}")
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToList();

        if (caseMismatches.Count > 0)
        {
            throw new InvalidOperationException(
                "Database contains ResourceKey rows whose casing differs from the canonical seed key. " +
                "Resolve them before seeding: " +
                string.Join(", ", caseMismatches.Take(20)));
        }
    }

    private static void EnsureDatabaseResourceValuesAreUnambiguous(IReadOnlyCollection<ResourceKey> resourceKeys)
    {
        List<string> duplicateValues = resourceKeys
            .SelectMany(resourceKey => resourceKey.Values
                .GroupBy(value => value.LanguageId)
                .Where(group => group.Count() > 1)
                .Select(group => $"{resourceKey.KeyName} / languageId={group.Key}"))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToList();

        if (duplicateValues.Count > 0)
        {
            throw new InvalidOperationException(
                "Database contains duplicate ResourceValue rows for the same key and language. Resolve them before seeding: " +
                string.Join(", ", duplicateValues.Take(20)));
        }
    }

    private static void UpsertResourceValue(
        ResourceKey resourceKey,
        Guid languageId,
        string culture,
        string value,
        DateTime utcNow,
        bool overwriteExisting,
        ref int translationsAdded,
        ref int translationsUpdated)
    {
        List<ResourceValue> matchingValues = resourceKey.Values
            .Where(item => item.LanguageId == languageId)
            .ToList();

        if (matchingValues.Count > 1)
        {
            throw new InvalidOperationException(
                $"Database contains {matchingValues.Count} ResourceValue rows for '{resourceKey.KeyName}' and '{culture}'.");
        }

        ResourceValue? resourceValue = matchingValues.SingleOrDefault();

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

            translationsAdded++;
            return;
        }

        // Always restore a soft-deleted translation. Existing non-empty panel values remain protected
        // unless OverwriteExistingTranslations is explicitly enabled.
        resourceValue.DeletedDate = null;
        resourceValue.DeletedBy = null;

        if (!overwriteExisting && !string.IsNullOrWhiteSpace(resourceValue.Value))
            return;

        if (string.Equals(resourceValue.Value, value, StringComparison.Ordinal))
            return;

        resourceValue.Value = value;
        resourceValue.UpdatedDate = utcNow;
        resourceValue.UpdatedBy = "System";
        translationsUpdated++;
    }

    private static string DisplayKey(string? keyName) =>
        string.IsNullOrWhiteSpace(keyName) ? "<empty>" : keyName;

    private static void AddIssue(
        ICollection<string> issues,
        string category,
        IReadOnlyCollection<string> values)
    {
        if (values.Count == 0)
            return;

        issues.Add($"{category}: {values.Count} [{string.Join(", ", values.Take(10))}]");
    }
}

public sealed record LocalizationSeedDefinitionAuditResult(
    IReadOnlyCollection<ResourceSeedDefinition> Resources,
    IReadOnlyCollection<string> ExactDuplicateKeys,
    IReadOnlyCollection<string> ValueConflictDuplicateKeys,
    IReadOnlyCollection<string> AreaConflictDuplicateKeys,
    IReadOnlyCollection<IReadOnlyCollection<string>> CaseInsensitiveDuplicateKeyGroups,
    IReadOnlyCollection<string> EmptyKeyDefinitions,
    IReadOnlyCollection<string> KeysWithLeadingOrTrailingWhitespace,
    IReadOnlyCollection<string> KeysWithEmptyArea,
    IReadOnlyCollection<string> KeysWithEmptyTurkishValue,
    IReadOnlyCollection<string> KeysWithEmptyEnglishValue)
{
    public bool IsValid =>
        ExactDuplicateKeys.Count == 0 &&
        ValueConflictDuplicateKeys.Count == 0 &&
        AreaConflictDuplicateKeys.Count == 0 &&
        CaseInsensitiveDuplicateKeyGroups.Count == 0 &&
        EmptyKeyDefinitions.Count == 0 &&
        KeysWithLeadingOrTrailingWhitespace.Count == 0 &&
        KeysWithEmptyArea.Count == 0 &&
        KeysWithEmptyTurkishValue.Count == 0 &&
        KeysWithEmptyEnglishValue.Count == 0;
}

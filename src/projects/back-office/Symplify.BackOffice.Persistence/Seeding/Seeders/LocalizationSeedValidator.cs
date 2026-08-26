using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Domain.Localization;
using Symplify.BackOffice.Persistence.Contexts;
using Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;
using Symplify.BackOffice.Persistence.Seeding.Options;

namespace Symplify.BackOffice.Persistence.Seeding.Seeders;

/// <summary>
/// Validates that all seed-defined localization keys and their tr-TR / en-US translations
/// are correctly persisted in the database. Reports duplicates, missing keys and orphan keys.
/// </summary>
public sealed class LocalizationSeedValidator
{
    private readonly BackOfficeDbContext _context;
    private readonly LocalizationSeedOptions _options;
    private readonly ILogger<LocalizationSeedValidator> _logger;

    public LocalizationSeedValidator(
        BackOfficeDbContext context,
        IOptions<LocalizationSeedOptions> options,
        ILogger<LocalizationSeedValidator> logger)
    {
        _context = context;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Runs a full validation against the database.
    /// Returns a result object; the caller decides whether to throw based on FailOnMissingTranslation.
    /// </summary>
    public async Task<LocalizationValidationResult> ValidateAsync(
        IReadOnlyCollection<ResourceSeedDefinition> seedDefinitions,
        CancellationToken cancellationToken = default)
    {
        LocalizationValidationResult result = new();

        _logger.LogInformation(
            "Localization validation started. Validating {Count} seed definition(s).",
            seedDefinitions.Count);

        // ── 1. Detect duplicate keys within seed definitions ──────────────────
        List<string> duplicateKeys = seedDefinitions
            .GroupBy(d => d.KeyName, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        result.DuplicateSeedKeys.AddRange(duplicateKeys);

        if (duplicateKeys.Count > 0)
        {
            _logger.LogWarning(
                "Localization validation: {Count} duplicate key(s) in seed definitions: {Keys}",
                duplicateKeys.Count,
                string.Join(", ", duplicateKeys));
        }

        // ── 2. Detect seed definitions with empty translation values ──────────
        List<string> emptyTrInSeed = seedDefinitions
            .Where(d => string.IsNullOrWhiteSpace(d.TurkishValue))
            .Select(d => d.KeyName)
            .ToList();

        List<string> emptyEnInSeed = seedDefinitions
            .Where(d => string.IsNullOrWhiteSpace(d.EnglishValue))
            .Select(d => d.KeyName)
            .ToList();

        result.EmptyTurkishInSeed.AddRange(emptyTrInSeed);
        result.EmptyEnglishInSeed.AddRange(emptyEnInSeed);

        if (emptyTrInSeed.Count > 0)
        {
            _logger.LogWarning(
                "Localization validation: {Count} seed definition(s) have empty tr-TR values: {Keys}",
                emptyTrInSeed.Count,
                string.Join(", ", emptyTrInSeed));
        }

        if (emptyEnInSeed.Count > 0)
        {
            _logger.LogWarning(
                "Localization validation: {Count} seed definition(s) have empty en-US values: {Keys}",
                emptyEnInSeed.Count,
                string.Join(", ", emptyEnInSeed));
        }

        // ── 3. Load languages and all DB keys with translations ───────────────
        Language? trLanguage = await _context.Languages
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.Culture == "tr-TR", cancellationToken);

        Language? enLanguage = await _context.Languages
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.Culture == "en-US", cancellationToken);

        List<ResourceKey> allDbKeys = await _context.ResourceKeys
            .IgnoreQueryFilters()
            .Include(k => k.Values)
            .ToListAsync(cancellationToken);

        HashSet<string> dbKeyNames = allDbKeys
            .Select(k => k.KeyName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        HashSet<string> uniqueSeedKeys = seedDefinitions
            .Select(d => d.KeyName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // ── 4. Detect seed keys that are not present in DB ───────────────────
        List<string> missingInDb = uniqueSeedKeys
            .Where(k => !dbKeyNames.Contains(k))
            .OrderBy(k => k)
            .ToList();

        result.MissingDbKeys.AddRange(missingInDb);

        if (missingInDb.Count > 0)
        {
            _logger.LogWarning(
                "Localization validation: {Count} key(s) defined in seed but missing from DB: {Keys}",
                missingInDb.Count,
                string.Join(", ", missingInDb));
        }

        // ── 5. Check tr-TR and en-US translations for each seeded DB key ──────
        List<string> missingTr = new();
        List<string> missingEn = new();

        foreach (ResourceKey dbKey in allDbKeys)
        {
            if (!uniqueSeedKeys.Contains(dbKey.KeyName))
                continue; // only validate seed-managed keys; orphans are reported separately

            if (trLanguage is not null)
            {
                ResourceValue? trValue = dbKey.Values
                    .FirstOrDefault(v => v.LanguageId == trLanguage.Id && v.DeletedDate == null);

                if (trValue is null || string.IsNullOrWhiteSpace(trValue.Value))
                    missingTr.Add(dbKey.KeyName);
            }

            if (enLanguage is not null)
            {
                ResourceValue? enValue = dbKey.Values
                    .FirstOrDefault(v => v.LanguageId == enLanguage.Id && v.DeletedDate == null);

                if (enValue is null || string.IsNullOrWhiteSpace(enValue.Value))
                    missingEn.Add(dbKey.KeyName);
            }
        }

        result.MissingTurkishTranslations.AddRange(missingTr);
        result.MissingEnglishTranslations.AddRange(missingEn);

        if (missingTr.Count > 0)
        {
            _logger.LogWarning(
                "Localization validation: {Count} key(s) missing tr-TR translation in DB: {Keys}",
                missingTr.Count,
                string.Join(", ", missingTr));
        }

        if (missingEn.Count > 0)
        {
            _logger.LogWarning(
                "Localization validation: {Count} key(s) missing en-US translation in DB: {Keys}",
                missingEn.Count,
                string.Join(", ", missingEn));
        }

        // ── 6. Report orphan keys (in DB but not in any seed definition) ──────
        List<string> orphanKeys = allDbKeys
            .Where(k => !uniqueSeedKeys.Contains(k.KeyName))
            .Select(k => k.KeyName)
            .OrderBy(k => k)
            .ToList();

        result.OrphanDbKeys.AddRange(orphanKeys);

        if (orphanKeys.Count > 0)
        {
            _logger.LogInformation(
                "Localization validation: {Count} key(s) exist in DB but are not in any seed definition " +
                "(may be user-managed or from a removed seed). NOT automatically deleted. " +
                "First 30: {Keys}",
                orphanKeys.Count,
                string.Join(", ", orphanKeys.Take(30)));
        }

        // ── 7. Determine overall result ───────────────────────────────────────
        result.IsValid =
            missingInDb.Count == 0 &&
            missingTr.Count == 0 &&
            missingEn.Count == 0 &&
            emptyTrInSeed.Count == 0 &&
            emptyEnInSeed.Count == 0;

        if (result.IsValid)
        {
            _logger.LogInformation(
                "Localization validation passed. " +
                "Seed keys: {SeedCount} | DB keys: {DbCount} | Orphan keys: {OrphanCount} | Duplicates: {DuplicateCount}",
                uniqueSeedKeys.Count,
                dbKeyNames.Count,
                orphanKeys.Count,
                duplicateKeys.Count);
        }
        else
        {
            int totalIssues =
                missingInDb.Count +
                missingTr.Count +
                missingEn.Count +
                emptyTrInSeed.Count +
                emptyEnInSeed.Count;

            _logger.LogWarning(
                "Localization validation found {IssueCount} issue(s). " +
                "MissingInDb: {MissingDb} | MissingTr: {MissingTr} | MissingEn: {MissingEn} | " +
                "EmptyTrInSeed: {EmptyTr} | EmptyEnInSeed: {EmptyEn}",
                totalIssues,
                missingInDb.Count,
                missingTr.Count,
                missingEn.Count,
                emptyTrInSeed.Count,
                emptyEnInSeed.Count);
        }

        return result;
    }
}

/// <summary>
/// Holds the outcome of a localization seed validation pass.
/// </summary>
public sealed class LocalizationValidationResult
{
    /// <summary>True when no missing keys, no missing translations and no empty seed values were found.</summary>
    public bool IsValid { get; set; }

    /// <summary>Keys that appear more than once across all seed definitions.</summary>
    public List<string> DuplicateSeedKeys { get; } = new();

    /// <summary>Seed definitions whose tr-TR value is null or whitespace.</summary>
    public List<string> EmptyTurkishInSeed { get; } = new();

    /// <summary>Seed definitions whose en-US value is null or whitespace.</summary>
    public List<string> EmptyEnglishInSeed { get; } = new();

    /// <summary>Keys present in seed definitions but absent from the DB ResourceKeys table.</summary>
    public List<string> MissingDbKeys { get; } = new();

    /// <summary>DB keys (from seed) that have no tr-TR ResourceValue or an empty one.</summary>
    public List<string> MissingTurkishTranslations { get; } = new();

    /// <summary>DB keys (from seed) that have no en-US ResourceValue or an empty one.</summary>
    public List<string> MissingEnglishTranslations { get; } = new();

    /// <summary>DB keys that exist in the database but are not declared in any seed definition.</summary>
    public List<string> OrphanDbKeys { get; } = new();
}

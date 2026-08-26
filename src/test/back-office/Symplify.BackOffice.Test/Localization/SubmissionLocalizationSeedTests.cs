using System.Reflection;
using Symplify.BackOffice.Application.Features.Submissions.Constants;
using Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;
using Symplify.BackOffice.Persistence.Seeding.Seeders;

namespace Symplify.BackOffice.Test.Localization;

public sealed class SubmissionLocalizationSeedTests
{
    [Fact]
    public void Submission_seed_definitions_should_not_have_duplicate_keys()
    {
        List<ResourceSeedDefinition> resources = GetSubmissionResources();

        List<string> duplicateKeys = resources
            .GroupBy(resource => resource.KeyName, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        Assert.Empty(duplicateKeys);
    }

    [Fact]
    public void Submission_seed_definitions_should_have_tr_and_en_values()
    {
        List<ResourceSeedDefinition> resources = GetSubmissionResources();

        List<string> invalidKeys = resources
            .Where(resource =>
                string.IsNullOrWhiteSpace(resource.KeyName) ||
                string.IsNullOrWhiteSpace(resource.TurkishValue) ||
                string.IsNullOrWhiteSpace(resource.EnglishValue))
            .Select(resource => resource.KeyName)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        Assert.Empty(invalidKeys);
    }

    [Fact]
    public void Submission_business_message_keys_should_exist_in_seed_definitions()
    {
        HashSet<string> seedKeys = GetSubmissionResources()
            .Select(resource => resource.KeyName)
            .ToHashSet(StringComparer.Ordinal);

        List<string> missingKeys = typeof(SubmissionsMessages)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .Where(key => !seedKeys.Contains(key))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        Assert.Empty(missingKeys);
    }

    [Fact]
    public void Localization_seed_audit_should_separate_identical_and_conflicting_duplicates()
    {
        ResourceSeedDefinition first = new("Area", "BackOffice.Test.Key", "TR", "EN");
        ResourceSeedDefinition identical = new("Area", "BackOffice.Test.Key", "TR", "EN");
        ResourceSeedDefinition conflict = new("Area", "BackOffice.Test.Conflict", "TR", "EN");
        ResourceSeedDefinition conflictWithDifferentValue = new("Area", "BackOffice.Test.Conflict", "TR2", "EN");
        ResourceSeedDefinition areaConflict = new("Area", "BackOffice.Test.AreaConflict", "TR", "EN");
        ResourceSeedDefinition areaConflictWithSameValue = new("OtherArea", "BackOffice.Test.AreaConflict", "TR", "EN");

        LocalizationSeedDefinitionAuditResult result = LocalizationResourceSeeder.ValidateSeedDefinitions(
            new[]
            {
                first,
                identical,
                conflict,
                conflictWithDifferentValue,
                areaConflict,
                areaConflictWithSameValue
            });

        Assert.Contains("BackOffice.Test.Key", result.IdenticalDuplicateKeys);
        Assert.Contains("BackOffice.Test.Conflict", result.ConflictDuplicateKeys);
        Assert.Contains("BackOffice.Test.AreaConflict", result.AreaConflictDuplicateKeys);
    }

    private static List<ResourceSeedDefinition> GetSubmissionResources()
    {
        return SubmissionResourceSeedDefinitions.All
            .Concat(SubmissionUiResourceSeedDefinitions.All)
            .Concat(SubmissionManagementUiResourceSeedDefinitions.All)
            .Concat(SubmissionFinalFileShortLinkResourceSeedDefinitions.All)
            .ToList();
    }
}

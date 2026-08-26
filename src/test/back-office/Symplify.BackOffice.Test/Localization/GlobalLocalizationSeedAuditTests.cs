using Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;
using Symplify.BackOffice.Persistence.Seeding.Seeders;
using System.Reflection;

namespace Symplify.BackOffice.Test.Localization;

public sealed class GlobalLocalizationSeedAuditTests
{
    [Fact]
    public void Global_seed_definitions_should_not_have_unexpected_duplicate_keys()
    {
        LocalizationSeedDefinitionAuditResult result = LocalizationResourceSeeder.ValidateSeedDefinitions(
            LocalizationResourceSeeder.GetAllResources());

        List<string> unexpectedIdenticalDuplicates = result.IdenticalDuplicateKeys
            .Where(key => !IsKnownUnresolvedDuplicate(key))
            .Select(FormatDuplicateKey)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        List<string> unexpectedAreaConflicts = result.AreaConflictDuplicateKeys
            .Where(key => !IsKnownUnresolvedDuplicate(key))
            .Select(FormatDuplicateKey)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        List<string> unexpectedValueConflicts = result.ConflictDuplicateKeys
            .Where(key => !IsKnownUnresolvedDuplicate(key))
            .Select(FormatDuplicateKey)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            unexpectedIdenticalDuplicates.Count == 0 &&
            unexpectedAreaConflicts.Count == 0 &&
            unexpectedValueConflicts.Count == 0,
            "Unexpected localization duplicate key(s). " +
            "Exact: " + string.Join(", ", unexpectedIdenticalDuplicates) +
            " | Area: " + string.Join(", ", unexpectedAreaConflicts) +
            " | Value: " + string.Join(", ", unexpectedValueConflicts));
    }

    [Fact]
    public void Global_seed_definitions_should_have_non_empty_area_key_and_values()
    {
        List<string> invalidDefinitions = LocalizationResourceSeeder.GetAllResources()
            .Where(resource =>
                string.IsNullOrWhiteSpace(resource.AreaName) ||
                string.IsNullOrWhiteSpace(resource.KeyName) ||
                string.IsNullOrWhiteSpace(resource.TurkishValue) ||
                string.IsNullOrWhiteSpace(resource.EnglishValue))
            .Select(FormatDefinition)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            invalidDefinitions.Count == 0,
            "Invalid localization seed definition(s): " + string.Join(" | ", invalidDefinitions));
    }

    [Fact]
    public void Global_seed_keys_should_not_have_surrounding_whitespace()
    {
        List<string> invalidKeys = LocalizationResourceSeeder.GetAllResources()
            .Where(resource => resource.KeyName != resource.KeyName.Trim())
            .Select(FormatDefinition)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            invalidKeys.Count == 0,
            "Localization key(s) with surrounding whitespace: " + string.Join(" | ", invalidKeys));
    }

    [Fact]
    public void Global_seed_keys_should_not_have_case_insensitive_collisions()
    {
        LocalizationSeedDefinitionAuditResult result = LocalizationResourceSeeder.ValidateSeedDefinitions(
            LocalizationResourceSeeder.GetAllResources());

        List<string> collisions = result.CaseInsensitiveDuplicateKeyGroups
            .Select(group => string.Join(", ", group.OrderBy(key => key, StringComparer.Ordinal)))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            collisions.Count == 0,
            "Case-insensitive localization key collision(s): " + string.Join(" | ", collisions));
    }

    private static string FormatDefinition(ResourceSeedDefinition resource)
    {
        return $"{resource.KeyName} [area={resource.AreaName}, tr={resource.TurkishValue}, en={resource.EnglishValue}]";
    }

    private static string FormatDuplicateKey(string key)
    {
        List<string> sources = GetSeedDefinitionSources()
            .Where(source => source.Resource.KeyName == key)
            .Select(source => $"{source.SourceFile} [area={source.Resource.AreaName}, tr={source.Resource.TurkishValue}, en={source.Resource.EnglishValue}]")
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToList();

        return $"{key} ({string.Join("; ", sources)})";
    }

    private static IEnumerable<SeedDefinitionSource> GetSeedDefinitionSources()
    {
        Type resourceDefinitionType = typeof(CommonResourceSeedDefinitions);
        IEnumerable<Type> definitionTypes = resourceDefinitionType.Assembly
            .GetTypes()
            .Where(type =>
                string.Equals(type.Namespace, resourceDefinitionType.Namespace, StringComparison.Ordinal) &&
                type.Name.EndsWith("ResourceSeedDefinitions", StringComparison.Ordinal))
            .OrderBy(type => type.Name, StringComparer.Ordinal);

        foreach (Type definitionType in definitionTypes)
        {
            PropertyInfo? allProperty = definitionType.GetProperty("All", BindingFlags.Public | BindingFlags.Static);

            if (allProperty?.GetValue(null) is not IEnumerable<ResourceSeedDefinition> resources)
                continue;

            foreach (ResourceSeedDefinition resource in resources)
                yield return new SeedDefinitionSource($"{definitionType.Name}.cs", resource);
        }
    }

    private static bool IsKnownUnresolvedDuplicate(string key)
    {
        return KnownUnresolvedDuplicateKeys.Contains(key) ||
            KnownUnresolvedDuplicatePrefixes.Any(prefix => key.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static readonly string[] KnownUnresolvedDuplicatePrefixes =
    {
        "BackOffice.Auth.Validation.",
        "BackOffice.AuthorTopbar.",
        "BackOffice.Home.",
        "BackOffice.LanguageSwitcher.",
        "BackOffice.Mail.",
        "BackOffice.Organization",
        "BackOffice.Submissions.Business.",
        "BackOffice.Topics.",
        "BackOffice.TransactionStatuses.",
        "BackOffice.UserMenu."
    };

    private sealed record SeedDefinitionSource(string SourceFile, ResourceSeedDefinition Resource);

    // Temporary technical debt: keep only exact unresolved duplicate keys here and remove each key when its seed conflict is fixed.
    private static readonly HashSet<string> KnownUnresolvedDuplicateKeys = new(StringComparer.Ordinal)
    {
        "BackOffice.Topics.ListTitle",
        "BackOffice.Topics.Placeholders.Name",
        "BackOffice.Topics.Placeholders.Description",
        "Common.GenericError",
        "Common.InvalidRequest",
        "Common.ReorderSuccess",
        "BackOffice.CongressPaymentPlans.Validation.CongressRequired",
        "BackOffice.CongressPaymentPlans.Validation.ValidFromInvalid",
        "BackOffice.CongressPaymentPlans.Validation.ValidUntilInvalid",
        "BackOffice.CongressPaymentPlans.Validation.DueDateInvalid",
        "BackOffice.CongressTopics.Messages.Saved",
        "BackOffice.CongressSubmissionTypes.Messages.Saved",
        "BackOffice.Congresses.Validation.LogoLightInvalidType",
        "BackOffice.Congresses.Validation.LogoDarkInvalidType",
        "BackOffice.Congresses.Validation.BannerInvalidType",
        "BackOffice.Congresses.Validation.LogoLightTooLarge",
        "BackOffice.Congresses.Validation.LogoDarkTooLarge",
        "BackOffice.Congresses.Validation.BannerTooLarge",
        "BackOffice.Congresses.Messages.Created",
        "BackOffice.Congresses.Messages.Updated",
        "BackOffice.Congresses.Messages.Deleted",
        "BackOffice.CongressSliders.Validation.CongressRequired",
        "BackOffice.CongressSliders.Validation.DisplayOrderGreaterThanZero",
        "BackOffice.CongressSliders.Validation.DisplayOrderAlreadyExists",
        "BackOffice.CongressSliders.Validation.DefaultTranslationRequired",
        "BackOffice.CongressSliders.Validation.ImageRequired",
        "BackOffice.CongressSliders.Validation.ImageInvalidType",
        "BackOffice.CongressSliders.Validation.ImageTooLarge",
        "BackOffice.CongressSliders.Validation.ObjectStorageBucketMissing",
        "BackOffice.CongressSliders.Validation.EntityNotFound",
        "BackOffice.CongressSliders.Validation.TranslationNotFound",
        "BackOffice.CongressSliders.Validation.DefaultTranslationCannotBeDeleted",
        "BackOffice.CongressSliders.Validation.AtLeastOneTranslationRequired",
        "BackOffice.CongressSliders.Validation.LanguageRequired",
        "BackOffice.CongressSliders.Validation.DuplicateTranslationLanguageInRequest",
        "BackOffice.CongressSliders.Messages.Created",
        "BackOffice.CongressSliders.Messages.Updated",
        "BackOffice.CongressSliders.Messages.Deleted",
        "BackOffice.CongressSliders.Messages.Reordered",
        "BackOffice.Congresses.Tabs.General",
        "BackOffice.Congresses.Tabs.GeneralDescription",
        "BackOffice.Congresses.Tabs.Localization",
        "BackOffice.Congresses.Tabs.LocalizationDescription",
        "BackOffice.Congresses.Tabs.Settings",
        "BackOffice.Congresses.Tabs.SettingsDescription",
        "BackOffice.Congresses.Tabs.Contact",
        "BackOffice.Congresses.Tabs.ContactDescription",
        "BackOffice.Congresses.Tabs.Branding",
        "BackOffice.Congresses.Tabs.BrandingDescription",
        "BackOffice.Congresses.Fields.Organization",
        "BackOffice.Congresses.Fields.Code",
        "BackOffice.Congresses.Fields.Slug",
        "BackOffice.Congresses.Fields.StartDate",
        "BackOffice.Congresses.Fields.EndDate",
        "BackOffice.Congresses.Fields.AbstractDeadline",
        "BackOffice.Congresses.Fields.FullTextDeadline",
        "BackOffice.Congresses.Fields.TimeZone",
        "BackOffice.Congresses.Fields.Mode",
        "BackOffice.Congresses.Fields.ContactEmail",
        "BackOffice.Congresses.Fields.ContactPhone",
        "BackOffice.Congresses.Fields.WebsiteUrl",
        "BackOffice.Congresses.Fields.LogoLight",
        "BackOffice.Congresses.Fields.LogoDark",
        "BackOffice.Congresses.Fields.Banner",
        "BackOffice.Congresses.Fields.PrimaryColor",
        "BackOffice.Congresses.Fields.AccentColor",
        "BackOffice.Congresses.Fields.IsPublished",
        "BackOffice.Congresses.Fields.Title",
        "BackOffice.Congresses.Fields.Description",
        "BackOffice.Congresses.Fields.WelcomeContent",
        "BackOffice.Congresses.Fields.Language",
        "BackOffice.Congresses.Create.PageTitle",
        "BackOffice.Congresses.Create.Header",
        "BackOffice.Congresses.Create.Subtitle",
        "BackOffice.Congresses.Create.Button",
        "BackOffice.Congresses.Edit.PageTitle",
        "BackOffice.Congresses.Edit.Header",
        "BackOffice.Congresses.Edit.Subtitle",
        "BackOffice.Congresses.Edit.Button",
        "BackOffice.Congresses.Placeholders.Code",
        "BackOffice.Congresses.Placeholders.Slug",
        "BackOffice.Congresses.Placeholders.TimeZone",
        "BackOffice.Congresses.Placeholders.ContactEmail",
        "BackOffice.Congresses.Placeholders.ContactPhone",
        "BackOffice.Congresses.Placeholders.WebsiteUrl",
        "BackOffice.Congresses.Placeholders.PrimaryColor",
        "BackOffice.Congresses.Placeholders.AccentColor",
        "BackOffice.Congresses.Placeholders.Title",
        "BackOffice.Congresses.Placeholders.Subtitle",
        "BackOffice.Congresses.Placeholders.Description",
        "BackOffice.Congresses.Placeholders.WelcomeContent",
        "BackOffice.Congresses.Help.Code",
        "BackOffice.Congresses.Help.Slug",
        "BackOffice.Congresses.Help.Mode",
        "BackOffice.Congresses.Help.LogoLight",
        "BackOffice.Congresses.Help.LogoDark",
        "BackOffice.Congresses.Help.Banner",
        "BackOffice.Congresses.Help.BrandColors",
        "BackOffice.Congresses.Help.IsPublished",
        "BackOffice.Congresses.ListTitle",
        "BackOffice.Congresses.ListDescription",
        "BackOffice.Congresses.Filter.Search",
        "BackOffice.Congresses.Filter.Organization",
        "BackOffice.Congresses.Filter.Status",
        "BackOffice.Congresses.Filter.Apply",
        "BackOffice.Congresses.Filter.Clear",
        "BackOffice.Congresses.Table.Code",
        "BackOffice.Congresses.Table.Title",
        "BackOffice.Congresses.Table.Organization",
        "BackOffice.Congresses.Table.Dates",
        "BackOffice.Congresses.Table.Status",
        "BackOffice.Congresses.Table.Actions",
        "BackOffice.Congresses.Empty.Title",
        "BackOffice.Congresses.Empty.Description",
        "BackOffice.Organizations.DeleteConfirmText",
        "BackOffice.Organizations.DeleteConfirmTextWithName",
        "BackOffice.Organizations.DeleteConfirmTitle",
        "BackOffice.Organizations.Validation.ObjectStorageBucketMissing",
        "BackOffice.Sidebar.Definitions",
        "BackOffice.Topics.Messages.NotFound",
        "Common.FileTooLarge",
        "Common.Saved",
        "BackOffice.OrganizationApiKeys.Messages.Created",
        "BackOffice.OrganizationApiKeys.Messages.Updated",
        "BackOffice.OrganizationApiKeys.Messages.Deleted",
        "BackOffice.OrganizationApiKeys.Messages.StatusChanged",
        "BackOffice.OrganizationApiKeys.Messages.InvalidApiKey",
        "BackOffice.OrganizationApiKeys.Messages.SecretCopied",
        "BackOffice.OrganizationApiKeys.Messages.SecretCopyWarning",
        "BackOffice.OrganizationApiKeys.Messages.KeyPrefixCopied",
        "BackOffice.OrganizationApiKeys.Messages.Rotated",
        "BackOffice.OrganizationApiKeys.Messages.Revoked",
        "BackOffice.OrganizationApiKeys.Messages.LastUsedUpdated",
        "BackOffice.OrganizationApiKeys.Validation.EntityNotFound",
        "BackOffice.OrganizationApiKeys.Validation.EnvironmentRequired",
        "BackOffice.OrganizationApiKeys.Validation.InvalidExpirationDate",
        "BackOffice.OrganizationApiKeys.Validation.KeyTypeRequired",
        "BackOffice.OrganizationApiKeys.Validation.OrganizationNotFound",
        "BackOffice.OrganizationApiKeys.Validation.OrganizationRequired",
        "BackOffice.OrganizationApiKeys.Validation.NameRequired",
        "BackOffice.OrganizationApiKeys.Validation.NameMaxLength",
        "BackOffice.OrganizationApiKeys.Validation.ExpiresAtInvalid",
        "BackOffice.OrganizationApiKeys.Validation.ExpiresAtMustBeFuture",
        "BackOffice.OrganizationApiKeys.Validation.AtLeastOneScopeRequired",
        "BackOffice.OrganizationApiKeys.Validation.ScopeRequired",
        "BackOffice.OrganizationApiKeys.Validation.ScopeInvalid",
        "BackOffice.OrganizationApiKeys.Validation.SecretHashRequired",
        "BackOffice.OrganizationApiKeys.Validation.SecretHashMaxLength",
        "BackOffice.OrganizationApiKeys.Validation.KeyPrefixRequired",
        "BackOffice.OrganizationApiKeys.Validation.KeyPrefixMaxLength",
        "BackOffice.OrganizationApiKeys.Validation.KeyPrefixAlreadyExists",
        "BackOffice.OrganizationApiKeys.Validation.CannotUseInactiveKey",
        "BackOffice.Mail.Submission.Accepted.Subject",
        "BackOffice.Mail.Submission.Accepted.Title",
        "BackOffice.Mail.Submission.Accepted.Body",
        "BackOffice.Mail.Submission.Accepted.Button",
        "BackOffice.Mail.Submission.Rejected.Subject",
        "BackOffice.Mail.Submission.Rejected.Title",
        "BackOffice.Mail.Submission.Rejected.Body",
        "BackOffice.Mail.Submission.Rejected.Button",
        "BackOffice.Mail.Submission.RevisionRequested.Subject",
        "BackOffice.Mail.Submission.RevisionRequested.Title",
        "BackOffice.Mail.Submission.RevisionRequested.Body",
        "BackOffice.Mail.Submission.RevisionRequested.Button",
        "BackOffice.Mail.Submission.Label.SubmissionNumber",
        "BackOffice.Mail.Submission.Label.SubmissionTitle",
        "BackOffice.Submissions.Manage.PageTitle",
        "BackOffice.Organizations.Messages.Created",
        "BackOffice.Organizations.Messages.Deleted",
        "BackOffice.Organizations.Messages.Updated",
        "BackOffice.Organizations.Validation.CodeAlreadyExists",
        "BackOffice.Organizations.Validation.CodeMaxLength",
        "BackOffice.Organizations.Validation.CodeRequired",
        "BackOffice.Organizations.Validation.EntityNotFound",
        "BackOffice.Organizations.Validation.InvalidCode",
        "BackOffice.Organizations.Validation.InvalidContactEmail",
        "BackOffice.Organizations.Validation.InvalidLogo",
        "BackOffice.Organizations.Validation.InvalidWebsiteUrl",
        "BackOffice.Organizations.Validation.NameMaxLength",
        "BackOffice.Organizations.Validation.NameRequired",
        "BackOffice.Organizations.Validation.ShortNameMaxLength",
        "BackOffice.Topics.CreateButton",
        "BackOffice.Topics.CreateModalTitle",
        "BackOffice.Topics.Fields.Description",
        "BackOffice.Topics.Fields.Name",
        "BackOffice.Topics.ListDescription",
        "BackOffice.Topics.ManagementDescription",
        "BackOffice.Topics.ManagementTitle",
        "BackOffice.Topics.Messages.Created",
        "BackOffice.Topics.Messages.DeleteConfirm",
        "BackOffice.Topics.Messages.Deleted",
        "BackOffice.Topics.Messages.Updated",
        "BackOffice.Topics.PageDescription",
        "BackOffice.Topics.PageTitle",
        "BackOffice.Topics.TranslationModalDescription",
        "BackOffice.Topics.UpdateModalTitle",
        "BackOffice.Topics.Validation.AtLeastOneTranslationRequired",
        "BackOffice.Topics.Validation.DefaultTranslationCannotBeDeleted",
        "BackOffice.Topics.Validation.DefaultTranslationRequired",
        "BackOffice.Topics.Validation.DescriptionMaxLength",
        "BackOffice.Topics.Validation.DuplicateTranslationNameInRequest",
        "BackOffice.Topics.Validation.EntityNotFound",
        "BackOffice.Topics.Validation.LanguageRequired",
        "BackOffice.Topics.Validation.NameAlreadyExists",
        "BackOffice.Topics.Validation.NameMaxLength",
        "BackOffice.Topics.Validation.TranslationNotFound",
        "BackOffice.TransactionStatuses.Messages.InvalidRecord",
        "BackOffice.TransactionStatuses.Validation.AtLeastOneTranslationRequired",
        "BackOffice.TransactionStatuses.Validation.CodeMaxLength",
        "BackOffice.TransactionStatuses.Validation.CodeRequired",
        "BackOffice.TransactionStatuses.Validation.DefaultTranslationRequired",
        "BackOffice.TransactionStatuses.Validation.LanguageRequired",
        "BackOffice.TransactionStatuses.Validation.NameRequired",
        "BackOffice.TransactionStatuses.Validation.PhaseRequired",
        "BackOffice.CongressSliders.Validation.TitleRequired",
        "BackOffice.CongressBoardMembers.Business.DefaultTranslationCannotBeDeleted",
        "BackOffice.CongressBoards.Validation.DefaultTranslationCannotBeDeleted",
        "BackOffice.CongressPaymentPlans.Buttons.New",
        "BackOffice.CongressPaymentPlans.Buttons.Save",
        "BackOffice.CongressPaymentPlans.Buttons.Update",
        "BackOffice.CongressPaymentPlans.Create.Title",
        "BackOffice.CongressSliders.Validation.InvalidReorderList",
        "BackOffice.CongressSubmissionTypes.Validation.EntityNotFound",
        "BackOffice.CongressSubmissionTypes.Validation.InvalidSelectionList",
        "BackOffice.CongressTopics.Validation.EntityNotFound",
        "BackOffice.CongressTopics.Validation.InvalidSelectionList",
        "BackOffice.Congresses.Validation.DefaultTranslationCannotBeDeleted",
        "BackOffice.Congresses.Validation.PublishDateRangeInvalid",
        "BackOffice.Congresses.Validation.TranslationNotFound",
        "BackOffice.CongressBoardMembers.Business.TranslationNotFound",
        "BackOffice.CongressBoardMembers.Help.PhotoCurrent",
        "BackOffice.CongressBoardMembers.Storage.BucketMissing",
        "BackOffice.CongressBoards.Buttons.New",
        "BackOffice.CongressBoards.Fields.Order",
        "BackOffice.CongressBoards.ListDescription",
        "BackOffice.CongressBoards.ListTitle",
        "BackOffice.CongressBoards.Validation.BoardHasMembers",
        "BackOffice.CongressBoards.Validation.DefaultTranslationRequired",
        "BackOffice.CongressBoards.Validation.EntityNotFound",
        "BackOffice.CongressBoards.Validation.TranslationNotFound",
        "BackOffice.CongressEvaluationCriteria.Validation.EntityNotFound",
        "BackOffice.CongressPaymentPlans.Code.Help",
        "BackOffice.CongressPaymentPlans.Create.Description",
        "BackOffice.CongressPaymentPlans.Fields.Order",
        "BackOffice.CongressPaymentPlans.Help",
        "BackOffice.CongressPaymentPlans.ListDescription",
        "BackOffice.CongressPaymentPlans.Placeholders.Description",
        "BackOffice.CongressPaymentPlans.Placeholders.Name",
        "BackOffice.CongressPaymentPlans.Update.Description",
        "BackOffice.CongressPaymentPlans.Update.Title",
        "BackOffice.CongressPaymentPlans.Validation.AmountRequired",
        "BackOffice.CongressPaymentPlans.Validation.AudienceTypeRequired",
        "BackOffice.CongressPaymentPlans.Validation.CurrencyRequired",
        "BackOffice.CongressPaymentPlans.Validation.PaymentCategoryRequired",
        "BackOffice.CongressSliders.Create.Description",
        "BackOffice.CongressSliders.ListDescription",
        "BackOffice.CongressSliders.ListTitle",
        "BackOffice.CongressSliders.Reorder.Help",
        "BackOffice.CongressSliders.Update.Description",
        "BackOffice.CongressSliders.Validation.CongressNotFound",
        "BackOffice.CongressSliders.Validation.ImageInvalid",
        "BackOffice.CongressSliders.Validation.OrderInvalid",
        "BackOffice.CongressSliders.Validation.ReorderRequired",
        "BackOffice.CongressSliders.Validation.TranslationTitleRequired",
        "BackOffice.CongressSubmissionTypes.Buttons.Manage",
        "BackOffice.CongressSubmissionTypes.Empty",
        "BackOffice.CongressSubmissionTypes.ListDescription",
        "BackOffice.CongressSubmissionTypes.ListTitle",
        "BackOffice.CongressSubmissionTypes.Modal.Description",
        "BackOffice.CongressSubmissionTypes.Modal.Title",
        "BackOffice.CongressSubmissionTypes.NoGlobalLookup",
        "BackOffice.CongressSubmissionTypes.SelectionTitle",
        "BackOffice.CongressSubmissionTypes.Validation.CongressNotFound",
        "BackOffice.CongressSubmissionTypes.Validation.CongressRequired",
        "BackOffice.CongressSubmissionTypes.Validation.SubmissionTypeNotFound",
        "BackOffice.CongressSubmissionTypes.Validation.SubmissionTypeRequired",
        "BackOffice.CongressTopics.Buttons.Manage",
        "BackOffice.CongressTopics.Empty",
        "BackOffice.CongressTopics.ListDescription",
        "BackOffice.CongressTopics.ListTitle",
        "BackOffice.CongressTopics.Modal.Description",
        "BackOffice.CongressTopics.Modal.Title",
        "BackOffice.CongressTopics.NoGlobalLookup",
        "BackOffice.CongressTopics.SelectionTitle",
        "BackOffice.CongressTopics.Validation.CongressNotFound",
        "BackOffice.CongressTopics.Validation.CongressRequired",
        "BackOffice.CongressTopics.Validation.TopicNotFound",
        "BackOffice.CongressTopics.Validation.TopicRequired",
    };
}

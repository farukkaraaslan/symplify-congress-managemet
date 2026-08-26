namespace Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;

public static class TransactionStatusValidationResourceSeedDefinitions
{
    // Canonical definitions live in BackOfficeWorkflowResourceSeedDefinitions and
    // CommonResourceSeedDefinitions. Keeping this collection empty prevents the
    // former compatibility file from re-introducing duplicate seed keys.
    public static IReadOnlyCollection<ResourceSeedDefinition> All { get; } =
        Array.Empty<ResourceSeedDefinition>();
}

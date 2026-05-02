using System.Reflection;

namespace Symplify.BackOffice.Application.Common.Authorization;

public static class OperationClaimCatalog
{
    public static IReadOnlyList<string> GetAll()
    {
        Assembly assembly = typeof(OperationClaimCatalog).Assembly;

        return assembly
            .GetTypes()
            .Where(type =>
                type.IsClass &&
                type.IsAbstract &&
                type.IsSealed &&
                type.Name.EndsWith("OperationClaims", StringComparison.Ordinal))
            .SelectMany(type =>
                type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                    .Where(field =>
                        field.IsLiteral &&
                        !field.IsInitOnly &&
                        field.FieldType == typeof(string))
                    .Select(field => field.GetRawConstantValue()?.ToString()))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value)
            .ToList();
    }
}

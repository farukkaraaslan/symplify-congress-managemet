using System.Reflection;
namespace Symplify.BackOffice.Application.Common.Localization;
public static class LocalizedEntityRuntimeHelper
{
    public static object? GetPropertyValue(object entity, string propertyName)
        => entity.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)?.GetValue(entity);
    public static void SetPropertyValue(object entity, string propertyName, object? value)
    {
        PropertyInfo? property = entity.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (property is null || !property.CanWrite) return;
        property.SetValue(entity, value);
    }
    public static void ApplyFieldDictionary(object translation, IEnumerable<string> fieldNames, IDictionary<string, string?> fields)
    {
        foreach (string fieldName in fieldNames)
            if (fields.TryGetValue(fieldName, out string? value))
                SetPropertyValue(translation, fieldName, string.IsNullOrWhiteSpace(value) ? null : value.Trim());
    }
    public static Dictionary<string, string?> ExtractFields(object? translation, IEnumerable<string> fieldNames)
    {
        Dictionary<string, string?> result = new(StringComparer.OrdinalIgnoreCase);
        foreach (string fieldName in fieldNames)
            result[fieldName] = translation is null ? null : GetPropertyValue(translation, fieldName)?.ToString();
        return result;
    }
    public static bool HasRequiredField(IDictionary<string, string?> fields, string fieldName)
        => fields.TryGetValue(fieldName, out string? value) && !string.IsNullOrWhiteSpace(value);
    public static bool HasAnyValue(IDictionary<string, string?> fields, IEnumerable<string> fieldNames)
        => fieldNames.Any(fieldName => fields.TryGetValue(fieldName, out string? value) && !string.IsNullOrWhiteSpace(value));
}

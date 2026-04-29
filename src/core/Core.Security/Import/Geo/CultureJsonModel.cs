using System.Text.Json.Serialization;

namespace Core.Utilities.Import.Geo;

public sealed class CultureJsonModel
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("languageCode")]
    public string? LanguageCode { get; set; }

    [JsonPropertyName("countryIso2")]
    public string? CountryIso2 { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("nativeName")]
    public string? NativeName { get; set; }

    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; }
}


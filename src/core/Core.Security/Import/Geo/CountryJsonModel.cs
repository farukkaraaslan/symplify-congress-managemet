using System.Text.Json.Serialization;

namespace Core.Utilities.Import.Geo;

public sealed class CountryJsonModel
{
    [JsonPropertyName("iso2")]
    public string? Iso2 { get; set; }

    [JsonPropertyName("iso3")]
    public string? Iso3 { get; set; }

    [JsonPropertyName("numeric_code")]
    public string? NumericCode { get; set; }

    [JsonPropertyName("phonecode")]
    public string? PhoneCode { get; set; }

    [JsonPropertyName("capital")]
    public string? Capital { get; set; }

    [JsonPropertyName("currency")]
    public string? CurrencyCode { get; set; }

    [JsonPropertyName("currency_name")]
    public string? CurrencyName { get; set; }

    [JsonPropertyName("currency_symbol")]
    public string? CurrencySymbol { get; set; }

    [JsonPropertyName("region")]
    public string? Region { get; set; }

    [JsonPropertyName("subregion")]
    public string? Subregion { get; set; }

    [JsonPropertyName("emoji")]
    public string? EmojiFlag { get; set; }

    [JsonPropertyName("flagCode")]
    public string? FlagCode { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("native")]
    public string? NativeName { get; set; }

    [JsonPropertyName("translations")]
    public Dictionary<string, string>? Translations { get; set; }
}

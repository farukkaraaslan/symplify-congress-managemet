using System.Text.Json.Serialization;

namespace Core.Utilities.Import.Geo;

public sealed class StateJsonModel
{
    [JsonPropertyName("id")]
    public int ExternalId { get; set; }

    [JsonPropertyName("country_code")]
    public string? CountryIso2 { get; set; }

    [JsonPropertyName("iso2")]
    public string? Iso2 { get; set; }

    [JsonPropertyName("iso3166_2")]
    public string? Iso3166_2 { get; set; }

    [JsonPropertyName("fips_code")]
    public string? FipsCode { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("level")]
    public string? Level { get; set; }

    [JsonPropertyName("parent_id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int? ParentStateExternalId { get; set; }

    [JsonPropertyName("native")]
    public string? NativeName { get; set; }

    [JsonPropertyName("latitude")]
    public string? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public string? Longitude { get; set; }

    [JsonPropertyName("timezone")]
    public string? TimeZone { get; set; }

    [JsonPropertyName("wikiDataId")]
    public string? WikiDataId { get; set; }

    [JsonPropertyName("population")]
    public long? Population { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("translations")]
    public Dictionary<string, string>? Translations { get; set; }
}

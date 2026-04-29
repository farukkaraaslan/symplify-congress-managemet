using System.Text.Json.Serialization;

namespace Core.Utilities.Import.Geo;

public sealed class RegionJsonModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("translations")]
    public Dictionary<string, string>? Translations { get; set; }
}


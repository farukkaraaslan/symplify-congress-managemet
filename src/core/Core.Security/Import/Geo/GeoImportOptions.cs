namespace Core.Utilities.Import.Geo;

public sealed class GeoImportOptions
{
    public string BasePath { get; set; } = "data/geo";
    public string RegionsFileName { get; set; } = "regions.json";

    public string CountriesFileName { get; set; } = "countries.json";
    public string StatesFileName { get; set; } = "states.json";
    public string CitiesFileName { get; set; } = "cities.json";
    public string CulturesFileName { get; set; } = "cultures.json";
    public string DefaultLanguageCode { get; set; } = "en";
}

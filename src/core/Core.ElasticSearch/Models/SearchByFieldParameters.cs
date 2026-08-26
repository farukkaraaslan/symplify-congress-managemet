namespace Core.ElasticSearch.Models;

public class SearchByFieldParameters : SearchParameters
{
    public required string FieldName { get; init; }
    public required string Value { get; init; }
}

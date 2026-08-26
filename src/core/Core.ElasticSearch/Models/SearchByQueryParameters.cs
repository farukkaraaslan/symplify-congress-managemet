namespace Core.ElasticSearch.Models;

public class SearchByQueryParameters : SearchParameters
{
    public required string QueryName { get; init; }
    public required string Query { get; init; }
    public required string[] Fields { get; init; }
}

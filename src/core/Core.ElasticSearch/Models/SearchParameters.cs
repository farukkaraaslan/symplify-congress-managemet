namespace Core.ElasticSearch.Models;

public class SearchParameters
{
    public required string IndexName { get; init; }
    public int From { get; set; } = 0;
    public int Size { get; set; } = 10;
}

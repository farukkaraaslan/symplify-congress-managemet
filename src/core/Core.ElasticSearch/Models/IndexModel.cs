namespace Core.ElasticSearch.Models;

public class IndexModel
{
    public required string IndexName { get; init; }
    public required string AliasName { get; init; }
    public int NumberOfReplicas { get; set; } = 3;
    public int NumberOfShards { get; set; } = 3;
}

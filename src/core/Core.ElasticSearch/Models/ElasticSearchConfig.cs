namespace Core.ElasticSearch.Models;

public class ElasticSearchConfig
{
    public required string ConnectionString { get; init; }
    public required string UserName { get; init; }
    public required string Password { get; init; }
}

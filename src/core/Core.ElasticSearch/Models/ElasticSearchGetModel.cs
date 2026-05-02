namespace Core.ElasticSearch.Models;

public class ElasticSearchGetModel<T>
{
    public required string ElasticId { get; init; }
    public required T Item { get; init; }
}

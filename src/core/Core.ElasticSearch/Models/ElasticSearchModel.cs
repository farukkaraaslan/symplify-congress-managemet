using Nest;

namespace Core.ElasticSearch.Models;

public class ElasticSearchModel
{
    public required Id ElasticId { get; init; }
    public required string IndexName { get; init; }
}

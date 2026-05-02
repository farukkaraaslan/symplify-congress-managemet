namespace Core.ElasticSearch.Models;

public class ElasticSearchInsertManyModel : ElasticSearchModel
{
    public required object[] Items { get; init; }
}

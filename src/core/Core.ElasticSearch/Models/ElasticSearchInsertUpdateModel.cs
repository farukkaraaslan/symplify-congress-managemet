namespace Core.ElasticSearch.Models;

public class ElasticSearchInsertUpdateModel : ElasticSearchModel
{
    public required object Item { get; init; }
}

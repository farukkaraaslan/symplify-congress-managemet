namespace Symplify.BackOffice.WebUI.Models.Shared.DataTables;

public sealed class DataTableReorderRequest
{
    public List<DataTableReorderItemRequest> Items { get; set; } = new();
}

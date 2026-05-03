namespace Symplify.BackOffice.WebUI.Models.Shared.DataTables;

public sealed class DataTableIntReorderRequest
{
    public int? TransactionStatusPhaseId { get; set; }

    public List<DataTableIntReorderItemRequest> Items { get; set; } = new();
}

namespace Symplify.BackOffice.WebUI.Models.Shared.DataTables;

public sealed class DataTableRequest
{
    public int Draw { get; set; }

    public int Start { get; set; }

    public int Length { get; set; }

    public DataTableSearchRequest? Search { get; set; }

    public List<DataTableOrderRequest> Order { get; set; } = new();

    public List<DataTableColumnRequest> Columns { get; set; } = new();
}

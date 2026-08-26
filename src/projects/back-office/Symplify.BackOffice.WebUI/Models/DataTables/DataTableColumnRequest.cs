namespace Symplify.BackOffice.WebUI.Models.Shared.DataTables;

public sealed class DataTableColumnRequest
{
    public string? Data { get; set; }

    public string? Name { get; set; }

    public bool Searchable { get; set; }

    public bool Orderable { get; set; }

    public DataTableSearchRequest? Search { get; set; }
}

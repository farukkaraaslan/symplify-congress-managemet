namespace Symplify.BackOffice.WebUI.Models.Shared.DataTables;

public sealed class DataTableOrderRequest
{
    public int Column { get; set; }

    public string Dir { get; set; } = "asc";
}

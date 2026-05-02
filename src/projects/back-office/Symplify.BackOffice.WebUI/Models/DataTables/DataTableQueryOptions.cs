namespace Symplify.BackOffice.WebUI.Models.Shared.DataTables;

public sealed class DataTableQueryOptions
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 200;

    public int Start { get; private init; }
    public int Page { get; private init; }
    public int PageSize { get; private init; }
    public string? SearchText { get; private init; }
    public string SortColumn { get; private init; } = "order";
    public string SortDirection { get; private init; } = "asc";

    public static DataTableQueryOptions From(
        DataTableRequest request,
        string defaultSortColumn,
        string defaultSortDirection,
        IReadOnlyCollection<string> allowedSortColumns)
    {
        int start = request.Start < 0 ? 0 : request.Start;

        int pageSize = request.Length <= 0
            ? DefaultPageSize
            : Math.Min(request.Length, MaxPageSize);

        int page = start / pageSize;

        return new DataTableQueryOptions
        {
            Start = start,
            Page = page,
            PageSize = pageSize,
            SearchText = NormalizeSearchText(request.Search?.Value),
            SortColumn = ResolveSortColumn(request, defaultSortColumn, allowedSortColumns),
            SortDirection = ResolveSortDirection(request, defaultSortDirection)
        };
    }

    private static string ResolveSortColumn(
        DataTableRequest request,
        string defaultSortColumn,
        IReadOnlyCollection<string> allowedSortColumns)
    {
        DataTableOrderRequest? order = request.Order.FirstOrDefault();

        if (order is null || order.Column < 0 || request.Columns.Count <= order.Column)
            return defaultSortColumn;

        DataTableColumnRequest column = request.Columns[order.Column];

        if (!column.Orderable)
            return defaultSortColumn;

        string? requestedColumn = !string.IsNullOrWhiteSpace(column.Name)
            ? column.Name
            : column.Data;

        if (string.IsNullOrWhiteSpace(requestedColumn))
            return defaultSortColumn;

        return allowedSortColumns.Contains(requestedColumn, StringComparer.OrdinalIgnoreCase)
            ? requestedColumn
            : defaultSortColumn;
    }

    private static string ResolveSortDirection(
        DataTableRequest request,
        string defaultSortDirection)
    {
        string? direction = request.Order.FirstOrDefault()?.Dir;

        if (string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase))
            return "desc";

        if (string.Equals(direction, "asc", StringComparison.OrdinalIgnoreCase))
            return "asc";

        return string.Equals(defaultSortDirection, "desc", StringComparison.OrdinalIgnoreCase)
            ? "desc"
            : "asc";
    }

    private static string? NormalizeSearchText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}

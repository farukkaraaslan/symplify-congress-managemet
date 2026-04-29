using System.Text;
using System.Linq.Dynamic.Core;
namespace Core.Persistence.Dynamic;

public static class IQueryableDynamicFilterExtensions
{
    private static readonly string[] _orders = { "asc", "desc" };
    private static readonly string[] _logics = { "and", "or" };

    private static readonly IDictionary<string, string> _operators = new Dictionary<string, string>
    {
        { "eq", "=" },
        { "neq", "!=" },
        { "lt", "<" },
        { "lte", "<=" },
        { "gt", ">" },
        { "gte", ">=" },
        { "isnull", "== null" },
        { "isnotnull", "!= null" },
        { "startswith", "StartsWith" },
        { "endswith", "EndsWith" },
        { "contains", "Contains" },
        { "doesnotcontain", "Contains" },
        { "in", "In" },
        { "between", "Between" }
    };

    public static IQueryable<T> ToDynamic<T>(this IQueryable<T> query, DynamicQuery dynamicQuery)
    {
        if (dynamicQuery.Filter is not null)
            query = Filter(query, dynamicQuery.Filter);
        if (dynamicQuery.Sort is not null && dynamicQuery.Sort.Any())
            query = Sort(query, dynamicQuery.Sort);
        else if (typeof(T).GetProperty("Id") != null)
            query = query.OrderBy("Id");
        return query;
    }

    private static IQueryable<T> Filter<T>(IQueryable<T> queryable, Filter filter)
    {
        IList<Filter> filters = GetAllFilters(filter);
        var values = new List<object>();
        foreach (var f in filters)
        {
            if (f.Operator == "in")
            {
                string raw = f.Value ?? throw new ArgumentException("Value cannot be null for 'in' operator");
                var inValues = raw.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(v => v.Trim());
                values.AddRange(inValues);
            }
            else if (f.Operator == "between")
            {
                string raw = f.Value ?? throw new ArgumentException("Value cannot be null for 'between' operator");
                var betweenValues = raw.Split(',', StringSplitOptions.None);
                if (betweenValues.Length != 2)
                    throw new ArgumentException("Invalid Value for 'between' operator");

                values.AddRange(betweenValues.Select(v => v.Trim()));
            }
            else if (f.Operator is "isnull" or "isnotnull")
            {
                continue;
            }
            else
            {
                values.Add(f.Value ?? throw new ArgumentException("Invalid Value"));
            }
        }

        string where = Transform(filter, filters);
        if (!string.IsNullOrEmpty(where))
            queryable = queryable.Where(where, values.ToArray());

        return queryable;
    }

    private static IQueryable<T> Sort<T>(IQueryable<T> queryable, IEnumerable<Sort> sort)
    {
        foreach (Sort item in sort)
        {
            if (string.IsNullOrEmpty(item.Field))
                throw new ArgumentException("Invalid Field");
            if (string.IsNullOrEmpty(item.Dir) || !_orders.Contains(item.Dir))
                throw new ArgumentException("Invalid Order Type");
        }

        if (sort.Any())
        {
            string ordering = string.Join(separator: ",", values: sort.Select(s => $"{s.Field} {s.Dir}"));
            return queryable.OrderBy(ordering);
        }

        return queryable;
    }

    public static IList<Filter> GetAllFilters(Filter filter)
    {
        List<Filter> filters = [];
        GetFilters(filter, filters);
        return filters;
    }

    private static void GetFilters(Filter filter, IList<Filter> filters)
    {
        filters.Add(filter);
        if (filter.Filters is not null && filter.Filters.Any())
            foreach (Filter item in filter.Filters)
                GetFilters(item, filters);
    }

    public static string Transform(Filter filter, IList<Filter> filters)
    {
        ValidateFilter(filter);

        int index = filters.IndexOf(filter);
        string comparison = _operators[filter.Operator];
        StringBuilder where = new();

        if (filter.Operator == "doesnotcontain")
        {
            where.Append(TransformDoesNotContain(filter, comparison, index));
        }
        else if (comparison is "StartsWith" or "EndsWith" or "Contains")
        {
            where.Append(TransformStringComparison(filter, comparison, index));
        }
        else if (filter.Operator == "in")
        {
            where.Append(TransformInOperator(filter, index));
        }
        else if (filter.Operator == "between")
        {
            where.Append(TransformBetweenOperator(filter, index));
        }
        else if (filter.Operator is "isnull" or "isnotnull")
        {
            where.Append(TransformNullCheck(filter, comparison));
        }
        else if (!string.IsNullOrEmpty(filter.Value))
        {
            where.Append($"np({filter.Field}) {comparison} @{index}");
        }

        if (filter.Logic is not null && filter.Filters is not null && filter.Filters.Any())
        {
            if (!_logics.Contains(filter.Logic))
                throw new ArgumentException("Invalid Logic");

            string innerWhere = string.Join(separator: $" {filter.Logic} ", value: filter.Filters.Select(f => Transform(f, filters)).ToArray());
            return $"{where} {filter.Logic} ({innerWhere})";
        }

        return where.ToString();
    }

    private static void ValidateFilter(Filter filter)
    {
        if (string.IsNullOrEmpty(filter.Field))
            throw new ArgumentException("Invalid Field");
        if (string.IsNullOrEmpty(filter.Operator) || !_operators.ContainsKey(filter.Operator))
            throw new ArgumentException("Invalid Operator");
        if (filter.Operator is not ("isnull" or "isnotnull") && string.IsNullOrWhiteSpace(filter.Value))
            throw new ArgumentException("Invalid Value");
    }

    private static string TransformDoesNotContain(Filter filter, string comparison, int index)
    {
        return $"(!np({filter.Field}).{comparison}(@{index}))";
    }

    private static string TransformStringComparison(Filter filter, string comparison, int index)
    {
        if (!filter.CaseSensitive)
        {
            return $"(np({filter.Field}).ToLower().{comparison}(@{index}.ToLower()))";
        }
        else
        {
            return $"(np({filter.Field}).{comparison}(@{index}))";
        }
    }

    private static string TransformInOperator(Filter filter, int index)
    {
        string raw = filter.Value ?? string.Empty;
        if (string.IsNullOrEmpty(raw))
            throw new ArgumentException("Value cannot be null for 'in' operator");

        var valueCount = raw.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
        var paramIndexes = Enumerable.Range(index, valueCount)
                                     .Select(i => $"@{i}")
                                     .ToArray();

        string field = filter.CaseSensitive
            ? $"np({filter.Field})"
            : $"np({filter.Field}).ToLower()";

        return $"{field} in ({string.Join(",", paramIndexes)})";
    }

    private static string TransformBetweenOperator(Filter filter, int index)
    {
        string raw = filter.Value ?? string.Empty;
        if (string.IsNullOrEmpty(raw))
            throw new ArgumentException("Value cannot be null for 'between' operator");

        var values = raw.Split(',', StringSplitOptions.None);
        if (values.Length != 2)
            throw new ArgumentException("Invalid Value for 'between' operator");

        var lowerBound = $"@{index}";
        var upperBound = $"@{index + 1}";

        return $"(np({filter.Field}) >= {lowerBound} and np({filter.Field}) <= {upperBound})";
    }

    private static string TransformNullCheck(Filter filter, string comparison)
    {
        return $"np({filter.Field}) {comparison}";
    }
}

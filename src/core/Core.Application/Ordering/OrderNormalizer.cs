namespace Core.Application.Ordering;

public sealed class OrderNormalizer : IOrderNormalizer
{
    public IReadOnlyList<TEntity> Normalize<TEntity>(
        IList<TEntity> orderedItems,
        Func<TEntity, int> getOrder,
        Action<TEntity, int> setOrder,
        int startFrom = 1)
    {
        if (orderedItems == null || orderedItems.Count == 0)
            return Array.Empty<TEntity>();

        var modified = new List<TEntity>();

        for (var i = 0; i < orderedItems.Count; i++)
        {
            var expectedOrder = startFrom + i;
            var current = orderedItems[i];

            if (getOrder(current) == expectedOrder)
                continue;

            setOrder(current, expectedOrder);
            modified.Add(current);
        }

        return modified;
    }
}

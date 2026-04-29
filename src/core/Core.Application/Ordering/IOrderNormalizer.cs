namespace Core.Application.Ordering;

public interface IOrderNormalizer
{
    IReadOnlyList<TEntity> Normalize<TEntity>(
        IList<TEntity> orderedItems,
        Func<TEntity, int> getOrder,
        Action<TEntity, int> setOrder,
        int startFrom = 1);
}

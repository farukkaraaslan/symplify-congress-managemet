using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Core.Persistence.Paging;

public static class IQueryablePaginateExtensions
{
    private static IQueryable<T> EnsureDeterministicOrderIfPossible<T>(IQueryable<T> source)
    {
        if (source is IOrderedQueryable<T>)
            return source;

        var idProp = typeof(T).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public);
        if (idProp == null)
            return source;

        return source.OrderBy(x => EF.Property<object>(x!, "Id"));
    }

    public static async Task<IPaginate<T>> ToPaginateAsync<T>(
        this IQueryable<T> source,
        int index,
        int size,
        int from = 0,
        CancellationToken cancellationToken = default
    )
    {
        if (from > index)
            throw new ArgumentException($"From: {from} > Index: {index}, must from <= Index");

        int count = await source.CountAsync(cancellationToken).ConfigureAwait(false);

        List<T> items;

        if (size == -1) // tümünü istiyorsa
        {

            items = await source.ToListAsync(cancellationToken).ConfigureAwait(false);
            size = count == 0 ? 1 : count; // pages hesaplaması için sıfırdan kaçın
        }
        else
        {
            source = EnsureDeterministicOrderIfPossible(source);
            items = await source
                .Skip((index - from) * size)
                .Take(size)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        Paginate<T> list = new()
        {
            Index = index,
            Size = size,
            From = from,
            Count = count,
            Items = items,
            Pages = size == 0 ? 1 : (int)Math.Ceiling(count / (double)size)
        };

        return list;
    }


    public static IPaginate<T> ToPaginate<T>(this IQueryable<T> source, int index, int size, int from = 0)
    {
        if (from > index)
            throw new ArgumentException($"From: {from} > Index: {index}, must from <= Index");

        int count = source.Count();
        source = EnsureDeterministicOrderIfPossible(source);
        var items = source.Skip((index - from) * size).Take(size).ToList();

        Paginate<T> list =
            new()
            {
                Index = index,
                Size = size,
                From = from,
                Count = count,
                Items = items,
                Pages = (int)Math.Ceiling(count / (double)size)
            };
        return list;
    }
}

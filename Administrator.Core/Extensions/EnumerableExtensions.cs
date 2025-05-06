namespace Administrator.Core;

public static class EnumerableExtensions
{
    public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> enumerable)
    {
        var list = new List<T>();
        await foreach (var item in enumerable)
        {
            list.Add(item);
        }

        return list;
    }

    public static async Task<T> FirstAsync<T>(this IAsyncEnumerable<T> enumerable)
    {
        await foreach (var first in enumerable)
        {
            return first;
        }

        throw new InvalidOperationException($"Empty {enumerable.GetType().Name}.");
    }
    
    public static HashSet<T> SymmetricExceptWith<T>(this IEnumerable<T> first, IEnumerable<T> second)
    {
        var hashSet = new HashSet<T>(first);
        hashSet.SymmetricExceptWith(second);
        return hashSet;
    }

    public static bool TryAddUnique<T>(this ICollection<T> collection, T item)
    {
        if (collection.Contains(item))
            return false;
        
        collection.Add(item);
        return true;
    }
}
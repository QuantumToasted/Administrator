namespace Administrator.Core;

public static class EnumerableExtensions
{
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
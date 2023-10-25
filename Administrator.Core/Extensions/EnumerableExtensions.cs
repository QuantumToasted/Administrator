namespace Administrator.Core;

public static class EnumerableExtensions
{
    public static HashSet<T> SymmetricExceptWith<T>(this IEnumerable<T> first, IEnumerable<T> second)
    {
        var hashSet = new HashSet<T>(first);
        hashSet.SymmetricExceptWith(second);
        return hashSet;
    }
}
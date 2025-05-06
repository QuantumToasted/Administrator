using Administrator.Core;

namespace Administrator.Database;

public static class DbModelExtensions
{
    internal static string FormatKey<TKey>(this IKeyedEntity<TKey> entity) where TKey : notnull
        => $"`[#{entity.Id}]`";
}
namespace Administrator.Database;

public static class DbModelExtensions
{
    internal static string FormatKey(this INumberKeyedDbEntity entity)
        => $"`[#{entity.Id}]`";
}
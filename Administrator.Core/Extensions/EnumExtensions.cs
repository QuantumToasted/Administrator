namespace Administrator.Core;

public static class EnumExtensions
{
    public static bool HasFlag<TEnum>(this TEnum? @enum, TEnum flag)
        where TEnum : struct, Enum
    {
        return @enum?.HasFlag(flag) == true;
    }
}
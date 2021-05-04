using System;

namespace Administrator.Extensions
{
    public static class EnumExtensions
    {
        public static bool Has<TEnum>(this TEnum @enum, TEnum flag)
            where TEnum : Enum
        {
            return @enum.HasFlag(flag);
        }
    }
}
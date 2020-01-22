using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Administrator.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDescription<TEnum>(this TEnum @enum)
            where TEnum : Enum
            => typeof(TEnum).GetField(@enum.ToString()).GetCustomAttribute<DescriptionAttribute>()?.Description;

        public static IEnumerable<TEnum> GetFlagValues<TEnum>(this TEnum @enum, bool includeDefaultValue = false)
            where TEnum : Enum
        {
            foreach (var flag in Enum.GetValues(typeof(TEnum)).Cast<TEnum>())
            {
                if (!@enum.HasFlag(flag) || flag.Equals(default(TEnum)) && !includeDefaultValue) continue;

                yield return flag;
            }
        }
    }
}
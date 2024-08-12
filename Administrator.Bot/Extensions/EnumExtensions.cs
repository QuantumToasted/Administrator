namespace Administrator.Bot;

public static class EnumExtensions
{
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
using Laylua;

namespace Administrator.Bot;

public static class LuaExtensions
{
    public static TValue? GetValueOrDefault<TKey, TValue>(this LuaTable table, TKey key)
        where TKey : notnull
        where TValue : class
    {
        return table.TryGetValue<TKey, TValue>(key, out var value) ? value : null;
    }
    
    public static string SetStringGlobal<T>(this Lua lua, string key, T value)
    {
        lua.SetGlobal(key, value);
        return key;
    }
}
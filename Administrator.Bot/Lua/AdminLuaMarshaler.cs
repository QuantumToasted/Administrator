using System.Diagnostics.CodeAnalysis;
using Disqord;
using Laylua;
using Laylua.Marshaling;
using Laylua.Moon;
using Qommon;
using static Laylua.Moon.LuaNative;

namespace Administrator.Bot;

public sealed unsafe class AdminLuaMarshaler : DefaultLuaMarshaler
{
    public override bool TryGetValue<T>(LuaThread thread, int stackIndex, LuaAllowedValueConversions allowedConversions, out T? obj,
        [NotNullWhen(false)] out LuaConversionError? error) where T : default
    {
        var clrType = typeof(T);
        if (clrType.TryGetNullableUnderlyingType(out var nullableType))
        {
            clrType = nullableType;
        }

        if (clrType == typeof(Snowflake))
        {
            return TryGetValue(thread, stackIndex, out obj, out error);
        }
        
        return base.TryGetValue(thread, stackIndex, allowedConversions, out obj, out error);
    }

    public override bool TryGetValue<T>(LuaThread thread, int stackIndex, out T? obj, [NotNullWhen(false)] out LuaConversionError? error) where T : default
    {
        var L = thread.State.L;
        var luaType = lua_type(L, stackIndex);
        
        var clrType = typeof(T);
        if (clrType.TryGetNullableUnderlyingType(out var nullableType))
        {
            clrType = nullableType;
        }

        if (luaType == LuaType.Number && clrType == typeof(Snowflake) && lua_isinteger(L, stackIndex))
        {
            var longValue = lua_tointeger(L, stackIndex);
            obj = (T) (object) (ulong) longValue;
            error = null;
            return true;
        }
        
        return base.TryGetValue(thread, stackIndex, out obj, out error);
    }

    public override void PushValue<T>(LuaThread thread, T obj)
    {
        var L = thread.State.L;
        
        if (obj is Snowflake)
        {
            lua_pushinteger(L, (long) (ulong) (object) obj);
            return;
        }
        
        base.PushValue(thread, obj);
    }
}
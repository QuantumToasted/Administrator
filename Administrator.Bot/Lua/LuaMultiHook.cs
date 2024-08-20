using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using Laylua.Moon;

using static Laylua.Moon.LuaNative;

namespace Administrator.Bot;

// combines MaxInstructionCountLuaHook and CancellationTokenLuaHook
public sealed unsafe class LuaMultiHook(CancellationToken cancellationToken) : LuaHook
{
    private const int MAX_INSTRUCTIONS = 10000;

    protected override LuaEventMask EventMask => LuaEventMask.Call | LuaEventMask.Return | LuaEventMask.Line | LuaEventMask.Count;

    protected override int InstructionCount => MAX_INSTRUCTIONS;
    
    protected override void Execute(lua_State* L, lua_Debug* ar)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            luaL_error(L, "Execution timed out.");
            return;
        }

        if (ar->@event != LuaEvent.Count)
            return;

        var functionName = GetCurrentFunction(L, ar);
        var message = $"The maximum instruction count of {InstructionCount} was exceeded by " +
                      $"{(!string.IsNullOrWhiteSpace(functionName) ? $"'{functionName}'" : "main code")}.";

        luaL_error(L, message);
    }

    private static string? GetCurrentFunction(lua_State* L, lua_Debug* ar)
    {
        string? function = null;
        lua_getinfo(L, "Sn", ar);

        char[]? rentedArray = null;
        try
        {
            scoped Span<char> nameSpan;
            if (ar->name != null)
            {
                var utf8NameSpan = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(ar->name);
                var charCount = Encoding.UTF8.GetCharCount(utf8NameSpan);
                nameSpan = charCount > 256
                    ? (rentedArray = ArrayPool<char>.Shared.Rent(charCount)).AsSpan(0, charCount)
                    : stackalloc char[charCount];

                Encoding.UTF8.GetChars(utf8NameSpan, nameSpan);
            }
            else
            {
                nameSpan = Span<char>.Empty;
            }

            function = nameSpan.IsEmpty || MemoryExtensions.IsWhiteSpace(nameSpan)
                ? null
                : nameSpan.ToString();
        }
        finally
        {
            if (rentedArray != null)
            {
                ArrayPool<char>.Shared.Return(rentedArray);
            }
        }

        return function;
    }
}
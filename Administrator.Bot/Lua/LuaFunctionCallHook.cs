using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Humanizer;
using Laylua;
using Laylua.Moon;
using Microsoft.Extensions.Logging;

using static Laylua.Moon.LuaNative;

namespace Administrator.Bot;

public sealed unsafe class LuaFunctionCallHook(ILogger<LuaFunctionCallHook> logger) : LuaHook
{
    private const int MAX_COMBINED_FUNCTION_CALLS = 25;
    private const int MAX_FUNCTION_CALLS = 5;

    private readonly ConcurrentDictionary<string, int> _calledFunctions = new();
    
    protected override void Execute(LuaThread thread, LuaEvent @event, ref LuaDebug debug)
    {
        logger.LogDebug("FunctionName: {Name}, FunctionTypeName: {TypeName}", debug.FunctionName, debug.FunctionTypeName);

        /*
        var functionName = string.Empty;
        if (!TryRateLimit(functionName, out var error))
        {
            luaL_error(thread.State.L, error);
        }
        */
    }

    protected override LuaEventMask EventMask => LuaEventMask.Call;
    protected override int InstructionCount => 0;
    
    private bool TryRateLimit(string memberName, [NotNullWhen(false)] out string? error)
    {
        var calls = _calledFunctions.GetOrAdd(memberName, 0);

        var maxFunctionCalls = MAX_FUNCTION_CALLS;

        if (memberName is "Get") // http calls
            maxFunctionCalls = 1;

        if (calls > maxFunctionCalls)
        {
            error = $"Maximum call count of {maxFunctionCalls} exceeded for function '{memberName.Camelize()}'";
            return false;
        }

        if (_calledFunctions.Values.Sum() > MAX_COMBINED_FUNCTION_CALLS)
        {
            error = $"Maximum total function call count {MAX_COMBINED_FUNCTION_CALLS} exceeded.";
            return false;
        }
        
        _calledFunctions[memberName] = calls + 1;
        error = null;
        return true;
    }
}
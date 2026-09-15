using Disqord.Bot.Commands.Application;
using Laylua;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Administrator.Bot;

public abstract class DiscordLuaLibrary : LuaLibrary
{
    private string[] _globals = null!;
    
    protected abstract IEnumerable<string> EnumerateGlobals(Lua lua);

    public override IReadOnlyList<string> Globals => _globals;

    protected sealed override void Open(Lua lua, bool leaveOnStack)
    {
        _globals = EnumerateGlobals(lua).ToArray();
    }
    
    protected sealed override void Close(Lua lua)
    { }
}

public abstract class DiscordLuaLibrary<TLibrary>(AdminLuaContext context) : DiscordLuaLibrary
    where TLibrary : DiscordLuaLibrary<TLibrary>
{
    protected ILogger Logger { get; } = context.CommandContext.Services.GetRequiredService<ILogger<TLibrary>>();

    protected AdminLuaContext Context { get; } = context;
    
    protected CancellationToken CancellationToken { get; } = context.Cts.Token;

    protected string RegisterType(string name, Type type)
    {
        Context.Lua[name] = type;
        return name;
    }
}
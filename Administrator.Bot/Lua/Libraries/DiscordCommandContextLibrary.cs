using Laylua;

namespace Administrator.Bot;

public sealed class DiscordCommandContextLibrary(AdminLuaContext context)
    : DiscordLuaLibrary<DiscordCommandContextLibrary>(context)
{
    public override string Name => "ctx";
    
    protected override IEnumerable<string> EnumerateGlobals(Lua lua)
    {
        var ctx = new LuaCommandContext(Context.CommandContext, lua);
        yield return lua.SetStringGlobal(nameof(ctx), ctx);
        yield return lua.SetStringGlobal<Func<long>>("now", () => DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }
}
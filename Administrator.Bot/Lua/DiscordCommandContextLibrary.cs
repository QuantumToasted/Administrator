using Disqord.Bot.Commands.Application;
using Laylua;
using Laylua.Marshaling;

namespace Administrator.Bot;

public sealed class DiscordCommandContextLibrary(IDiscordApplicationGuildCommandContext context, CancellationToken cancellationToken) 
    : DiscordLuaLibraryBase(cancellationToken)
{
    public override string Name => "context";
    
    protected override IEnumerable<string> RegisterGlobals(Lua lua)
    {
        var ctx = new LuaCommandContext(context, lua, this);
        yield return lua.SetStringGlobal(nameof(ctx), ctx);
        yield return lua.SetStringGlobal("now", () => DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    static DiscordCommandContextLibrary()
    {
        UserDataDescriptorProvider.Default.SetModelDescriptors();
    }
}
using Disqord.Bot.Commands.Application;
using Laylua;

namespace Administrator.Bot;

public sealed class DiscordMenuLibrary(AdminLuaContext context) : DiscordLuaLibrary<DiscordMenuLibrary>(context)
{
    public override string Name => "menu";
    
    protected override IEnumerable<string> EnumerateGlobals(Lua lua)
    {
        yield return RegisterType("Menu", typeof(LuaMenu));
    }
}
using Disqord.Bot.Commands.Application;
using Laylua;

namespace Administrator.Bot;

public sealed class DiscordMenuLibrary(IDiscordApplicationGuildCommandContext context, 
    CancellationToken cancellationToken) : DiscordLuaLibraryBase(cancellationToken)
{
    public override string Name => "menu";
    
    protected override IEnumerable<string> RegisterGlobals(Lua lua)
    {
        using var menuTable = lua.CreateTable();
        menuTable.SetValue("create", CreateMenu);
        
        yield return lua.SetStringGlobal("Menu", menuTable);
    }

    public LuaMenu CreateMenu(LuaTable buttons, LuaTable msg, LuaFunction callback)
    {
        return new LuaMenu(buttons, msg, callback);
    }
}
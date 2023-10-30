using Disqord;
using Disqord.Bot;
using Laylua;

namespace Administrator.Bot;

public sealed partial class DiscordRestLuaLibrary(DiscordBotBase bot, Snowflake guildId) : DiscordLuaLibraryBase
{
    public override string Name => "rest";
    
    protected override IEnumerable<string> RegisterGlobals(Lua lua)
    {
        return RegisterChannelRestMethods(lua)
            .Concat(RegisterMessageRestMethods(lua))
            .Concat(RegisterUserRestMethods(lua))
            .Concat(RegisterRoleRestMethods(lua));
    }

    private partial IEnumerable<string> RegisterChannelRestMethods(Lua lua);
    private partial IEnumerable<string> RegisterMessageRestMethods(Lua lua);
    private partial IEnumerable<string> RegisterUserRestMethods(Lua lua);
    private partial IEnumerable<string> RegisterRoleRestMethods(Lua lua);
}
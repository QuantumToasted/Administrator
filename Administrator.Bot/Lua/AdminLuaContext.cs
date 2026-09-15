using Laylua.Marshaling;
using Disqord.Bot.Commands.Application;
using Disqord.Utilities.Threading;
using Laylua;
using Microsoft.Extensions.DependencyInjection;

[assembly: LuaTypeDefaults(NamingPolicy = LuaNamingPolicy.CamelCase)]

namespace Administrator.Bot;

public sealed class AdminLuaContext : IDisposable
{
    public AdminLuaContext(IDiscordApplicationGuildCommandContext commandContext)
    {
        Cts = Cts.Linked(commandContext.Bot.StoppingToken);
        CommandContext = commandContext;
        Lua = new Lua(new AdminLuaMarshaler());
        
        Lua.OpenLibrary(LuaLibraries.Standard.Math);
        Lua.OpenLibrary(LuaLibraries.Standard.Base);
        Lua.OpenLibrary(LuaLibraries.Standard.String);
        Lua.OpenLibrary(LuaLibraries.Standard.Table);
        Lua.OpenLibrary(new DiscordCommandContextLibrary(this));
        Lua.OpenLibrary(new DiscordEnumLibrary(commandContext.Services.GetRequiredService<EmojiService>()));
        Lua.OpenLibrary(new DiscordHttpLibrary(this));
        Lua.OpenLibrary(new DiscordJsonLibrary());
        Lua.OpenLibrary(new DiscordPersistenceLibrary(this));
        Lua.OpenLibrary(new DiscordMenuLibrary(this));
    }

    public Cts Cts { get; }
    
    public Lua Lua { get; }

    public IDiscordApplicationGuildCommandContext CommandContext { get; }

    public void Dispose()
    {
        Lua.Dispose();
        Cts.Dispose();
    }
}
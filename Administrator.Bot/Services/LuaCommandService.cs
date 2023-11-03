using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Bot.Hosting;
using Microsoft.EntityFrameworkCore;
using Qmmands;

namespace Administrator.Bot;

public sealed class LuaCommandService : DiscordBotService
{
    public Dictionary<Snowflake, IModule> LuaCommandModules { get; } = new();

    public async Task ReloadLuaCommandsAsync(Snowflake guildId/*, bool external = false*/)
    {
        if (LuaCommandModules.Remove(guildId, out var module))
            Bot.Commands.RemoveModule(module);
        
        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var luaCommands = await db.LuaCommands.Where(x => x.GuildId == guildId).ToListAsync();

        if (luaCommands.Count == 0)
            return;

        module = BuildModule(guildId, luaCommands);
        LuaCommandModules[guildId] = module;
        Bot.Commands.AddModule(module);
    }

    private ApplicationModule BuildModule(Snowflake guildId, IEnumerable<LuaCommand> luaCommands)
    {
        var module = new ApplicationModuleBuilder
        {
            Name = guildId.ToString(),
            Description = $"Lua commands for guild {guildId}.",
            Checks = { new RequireGuildAttribute(guildId) }
        };

        foreach (var luaCommand in luaCommands)
        {
            var subModule = luaCommand.ToApplicationModule(Bot, module);
            module.Submodules.Add(subModule);
        }

        return module.Build();
    }
}
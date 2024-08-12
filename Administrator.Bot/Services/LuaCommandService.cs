using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Bot.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qmmands;

namespace Administrator.Bot;

public sealed class LuaCommandService : DiscordBotService
{
    private bool _forceSync;
    
    public Dictionary<Snowflake, IModule> LuaCommandModules { get; } = new();

    public async Task ReloadLuaCommandsAsync(Snowflake guildId)
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

        _forceSync = true;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

            if (!_forceSync)
                continue;

            try
            {
                await Bot.InitializeApplicationCommands(stoppingToken);
                Logger.LogInformation("Application commands re-synced!");
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to re-sync application commands.");
            }

            _forceSync = false;
        }
    }

    private ApplicationModule BuildModule(Snowflake guildId, IEnumerable<LuaCommand> luaCommands)
    {
        var module = new ApplicationModuleBuilder
        {
            Name = $"LUA_{guildId}",
            Description = $"Lua commands for guild {guildId}.",
            Checks = { new RequireGuildAttribute(guildId) }
        };

        foreach (var luaCommand in luaCommands)
        {
            luaCommand.MutateApplicationModule(Bot, module);
            //var subModule = luaCommand.ToApplicationModule(Bot, module);
            //module.Submodules.Add(subModule);
        }

        return module.Build();
    }
}
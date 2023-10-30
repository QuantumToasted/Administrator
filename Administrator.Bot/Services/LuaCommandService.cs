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

    /*
    private readonly Dictionary<Snowflake, ICommandService> _commandServices = new();

    public async Task UpdateLuaCommandsAsync(Snowflake guildId)
    {
        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        
        var customCommands = await db.LuaCommands.Where(x => x.GuildId == guildId).ToListAsync();

        if (customCommands.Count == 0)
            return;

        var commandMapProvider = Bot.Services.GetRequiredService<ICommandMapProvider>();
        var newMapProvider = new DefaultCommandMapProvider { new ApplicationCommandMap() };

        var commandService = new DefaultCommandService(
            Bot.Services.GetRequiredService<IOptions<DefaultCommandServiceConfiguration>>(),
            Bot.Services.GetRequiredService<ILogger<DefaultCommandService>>(),
            newMapProvider,
            Bot.Services);
        
        DefaultBotCommandsSetup.Initialize(commandService);
            
        var moduleBuilder = new ApplicationModuleBuilder
        {
            Name = guildId.ToString(),
            Description = $"Lua commands for guild {guildId}.",
            Checks = { new RequireGuildAttribute(guildId) }
        };

        var slashCommands = new List<LocalSlashCommand>();
        foreach (var customCommand in customCommands)
        {
            var slashCommand = customCommand.ToSlashCommand();
            slashCommands.Add(slashCommand);
                
            var commandBuilder = new ApplicationCommandBuilder(moduleBuilder, new DelegateCommandCallback(customCommand.ExecuteAsync))
            {
                Alias = slashCommand.Name.Value, // always set
                Description = slashCommand.Description.Value, // always set, or "No description."
            };
                
            moduleBuilder.Commands.Add(commandBuilder);
        }

        var module = moduleBuilder.Build();

        commandService.AddModule(module);
        await Bot.SetGuildApplicationCommandsAsync(Bot.CurrentUser.Id, guildId, slashCommands);

        _commandServices[guildId] = commandService;
    }

    protected override async ValueTask OnInteractionReceived(InteractionReceivedEventArgs e)
    {
        if (e.GuildId is not { } guildId || e.Interaction is not ISlashCommandInteraction { CommandGuildId: not null } interaction)
            return;

        if (_commandServices.TryGetValue(guildId, out var commandService))
        {
            var commandContext = Bot.CreateInteractionCommandContext(interaction);
            
            try
            {
                var result = await commandService.ExecuteAsync(commandContext);
                Logger.LogInformation("Result type: {Type}", result.GetType());
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to exec custom command.");
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Bot.WaitUntilReadyAsync(stoppingToken);

        foreach (var guildId in Bot.GetGuilds().Keys)
        {
            await UpdateLuaCommandsAsync(guildId);
        }
    }
    */
}
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Components;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qmmands;
using Qommon;

namespace Administrator.Bot;

public sealed class ButtonRoleService : DiscordBotService
{
    public Dictionary<Snowflake, IModule> ButtonCommandModules { get; } = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Bot.WaitUntilReadyAsync(stoppingToken);
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // avoids a race condition with EmojiService

        foreach (var guildId in Bot.GetGuilds().Keys)
        {
            try
            {
                await ReloadButtonCommandsAsync(guildId, null, null);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to reload button commands for guild {GuildId}.", guildId.RawValue);
            }
        }
    }
    
    public async Task ReloadButtonCommandsAsync(Snowflake guildId, Snowflake? channelId, Snowflake? messageId)
    {
        if (ButtonCommandModules.Remove(guildId, out var module))
            Bot.Commands.RemoveModule(module);

        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var buttonRoles = await db.ButtonRoles.Where(x => x.GuildId == guildId).ToListAsync();

        if (buttonRoles.Count == 0)
            return;

        module = BuildModule(guildId, buttonRoles);
        ButtonCommandModules[guildId] = module;
        Bot.Commands.AddModule(module);

        if (channelId.HasValue) // channelId and messageId are both set
        {
            var messageButtons = await db.ButtonRoles.Where(x => x.MessageId == messageId).ToListAsync();
            if (messageButtons.Count == 0) // all button roles have been removed
            {
                _ = Bot.ModifyMessageAsync(channelId.Value, messageId!.Value, x => x.Components = new List<LocalRowComponent>());
            }
            else
            {
                await AddButtonsAsync(messageButtons);
            }
        }
        else
        {
            var groups = await db.ButtonRoles.Where(x => x.GuildId == guildId)
                .GroupBy(x => x.MessageId).ToListAsync();

            foreach (var group in groups)
            {
                await AddButtonsAsync(group);
            }
        }
    }

    private async Task AddButtonsAsync(IEnumerable<ButtonRole> enumerable)
    {
        var buttonRoles = enumerable.ToList();
        var first = buttonRoles.First();
        Guard.IsTrue(buttonRoles.All(x => x.MessageId == first.MessageId));

        var (channelId, messageId) = (first.ChannelId, first.MessageId);
        
        var components = new List<LocalRowComponent>();
        foreach (var rowGroup in buttonRoles.GroupBy(x => x.Row))
        {
            Guard.HasSizeLessThanOrEqualTo(rowGroup.ToList(), 5);
            
            var row = new LocalRowComponent();
            foreach (var buttonRole in rowGroup.OrderBy(x => x.Position))
            {
                var button = buttonRole.ToButton(Bot);
                row.AddComponent(button);
            }
            
            components.Add(row);
        }
        
        await Bot.ModifyMessageAsync(channelId, messageId, x => x.Components = components);

        await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(1, 5)));
    }

    private ComponentModule BuildModule(Snowflake guildId, IEnumerable<ButtonRole> buttonRoles)
    {
        var module = new ComponentModuleBuilder
        {
            Name = $"BUTTONROLE_{guildId}",
            Description = $"Button role commands for guild {guildId}.",
            Checks = { new RequireGuildAttribute(guildId) }
        };

        foreach (var buttonRole in buttonRoles)
        {
            var command = new ComponentCommandBuilder(module, new DelegateCommandCallback(buttonRole.ExecuteAsync))
            {
                Name = buttonRole.GetCustomId(),
                Pattern = buttonRole.GetCustomId()
            };
            
            module.Commands.Add(command);
        }

        return module.Build();
    }
}
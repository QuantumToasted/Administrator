using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Extensions.Interactivity.Menus;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Bot;

public sealed class GuildConfigurationMainMenuView(IDiscordApplicationGuildCommandContext context) 
    : ViewBase(x => x.WithContent("Main Menu"))
{
    [Selection(Placeholder = "Select what to configure...", MaximumSelectedOptions = 1, MinimumSelectedOptions = 1)]
    [SelectionOption(LoggingChannelConfigurationView.SELECTION_TEXT)]
    [SelectionOption(SettingConfigurationView.SELECTION_TEXT)]
    [SelectionOption(InviteFilterExemptionConfigurationView.SELECTION_TEXT)]
    [SelectionOption(AutomaticPunishmentConfigurationView.SELECTION_TEXT)]
    [SelectionOption(XpExemptChannelsConfigurationView.SELECTION_TEXT)]
    [SelectionOption(TagLimitConfigurationView.SELECTION_TEXT)]
    [SelectionOption(AutoQuoteExemptChannelsConfigurationView.SELECTION_TEXT)]
    public async ValueTask SelectViewAsync(SelectionEventArgs e)
    {
        var text = e.SelectedOptions[0].Label.Value;
        await using var scope = context.Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        
        switch (text)
        {
            case LoggingChannelConfigurationView.SELECTION_TEXT:
            {
                var loggingChannels = await db.LoggingChannels
                    .Where(x => x.GuildId == context.GuildId)
                    .ToListAsync();

                await Menu.SetViewAsync(new LoggingChannelConfigurationView(context, loggingChannels));
                return;
            }
            case SettingConfigurationView.SELECTION_TEXT:
            {
                var guildConfig = await db.Guilds.GetOrCreateAsync(context.GuildId);
                await Menu.SetViewAsync(new SettingConfigurationView(context, guildConfig));
                return;
            }
            case InviteFilterExemptionConfigurationView.SELECTION_TEXT:
            {
                var exemptions = await db.InviteFilterExemptions.Where(x => x.GuildId == context.GuildId)
                    .ToListAsync();
                await Menu.SetViewAsync(new InviteFilterExemptionConfigurationView(context, exemptions));
                return;
            }
            case AutomaticPunishmentConfigurationView.SELECTION_TEXT:
            {
                var warningPunishments = await db.AutomaticPunishments.Where(x => x.GuildId == context.GuildId)
                    .OrderBy(x => x.DemeritPoints)
                    .ToListAsync();
                await Menu.SetViewAsync(new AutomaticPunishmentConfigurationView(context, warningPunishments));
                return;
            }
            case XpExemptChannelsConfigurationView.SELECTION_TEXT:
            {
                var guildConfig = await db.Guilds.GetOrCreateAsync(context.GuildId);
                await Menu.SetViewAsync(new XpExemptChannelsConfigurationView(context, guildConfig.XpExemptChannelIds));
                return;
            }
            case TagLimitConfigurationView.SELECTION_TEXT:
            {
                var guildConfig = await db.Guilds.GetOrCreateAsync(context.GuildId);
                await Menu.SetViewAsync(new TagLimitConfigurationView(context, guildConfig.MaximumTagsPerUser));
                return;
            }
            case AutoQuoteExemptChannelsConfigurationView.SELECTION_TEXT:
            {
                var guildConfig = await db.Guilds.GetOrCreateAsync(context.GuildId);
                await Menu.SetViewAsync(new AutoQuoteExemptChannelsConfigurationView(context, guildConfig.AutoQuoteExemptChannelIds));
                return;
            }
        }
    }
}
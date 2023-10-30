using System.Text;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Gateway;
using Disqord.Rest;

namespace Administrator.Bot;

public sealed class XpExemptChannelsConfigurationView(IDiscordApplicationGuildCommandContext context,
        HashSet<Snowflake> exemptChannelIds)
    : GuildConfigurationViewBase(context)
{
    public const string SELECTION_TEXT = "XP Exempt Channels";

    protected override string FormatContent()
    {
        var builder = new StringBuilder(SELECTION_TEXT);
        if (exemptChannelIds.Count > 0)
        {
            builder.AppendNewline()
                .AppendNewline(Markdown.Bold("Currently exempt channels:"))
                .AppendJoin('\n',
                    exemptChannelIds.Select(x =>
                        _context.Bot.GetChannel(_context.GuildId, x)?.Mention ?? x.ToString()));
        }
        else
        {
            builder.AppendNewline()
                .AppendNewline("No channels have been exempted from XP tracking.");
        }

        return builder.ToString();
    }

    [Selection(Placeholder = "Add channels", 
        Type = SelectionComponentType.Channel, ChannelTypes = new[] { ChannelType.Text, ChannelType.PublicThread }, 
        MaximumSelectedOptions = 20,
        MinimumSelectedOptions = 1)]
    public async ValueTask AddChannelsAsync(SelectionEventArgs e)
    {
        const int maximumChannels = 50;
        foreach (var entity in e.SelectedEntities)
        {
            if (exemptChannelIds.Count >= maximumChannels)
            {
                await e.Interaction.Response().SendMessageAsync(new LocalInteractionMessageResponse()
                    .WithContent($"You cannot add more than {Markdown.Bold(maximumChannels.ToString())} exempt channels!")
                    .WithIsEphemeral());
            }

            exemptChannelIds.Add(entity.Id);
        }
        
        await UpdateExemptChannelsAsync();
    }
    
    [Selection(Placeholder = "Remove channels", 
        Type = SelectionComponentType.Channel, ChannelTypes = new[] { ChannelType.Text, ChannelType.PublicThread },
        MaximumSelectedOptions = 20,
        MinimumSelectedOptions = 1)]
    public async ValueTask RemoveChannelsAsync(SelectionEventArgs e)
    {
        exemptChannelIds.Remove(e.SelectedEntities[0].Id);
        await UpdateExemptChannelsAsync();
    }

    private async Task UpdateExemptChannelsAsync()
    {
        await using var scope = _context.Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var guildConfig = await db.GetOrCreateGuildConfigAsync(_context.GuildId);
        guildConfig.XpExemptChannelIds = exemptChannelIds;
        await db.SaveChangesAsync();
        ReportChanges();
    }
}
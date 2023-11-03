using Administrator.Database;
using Disqord;
using Disqord.Bot;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.Extensions.DependencyInjection;

namespace Administrator.Bot;

public sealed class HighlightView : ViewBase
{
    private readonly IGatewayUserMessage _message;
    
    public HighlightView(IGatewayUserMessage message, IMessageGuildChannel channel, Action<LocalMessageBase>? messageTemplate) 
        : base(messageTemplate)
    {
        _message = message;
        
        AddComponent(new LinkButtonViewComponent(message.GetJumpUrl())
        {
            Label = "Jump to message"
        });

        AddComponent(new ButtonViewComponent(BlacklistAuthorAsync)
        {
            Label = $"Blacklist {message.Author.Tag}",
            Style = LocalButtonComponentStyle.Danger
        });

        AddComponent(new ButtonViewComponent(BlacklistChannelAsync)
        {
            Label = $"Blacklist #{channel.Name}",
            Style = LocalButtonComponentStyle.Danger
        });

        AddComponent(new ButtonViewComponent(DismissAsync)
        {
            Label = "Dismiss",
            Style = LocalButtonComponentStyle.Primary
        });
        
        AddComponent(new SelectionViewComponent(SnoozeAsync)
        {
            Placeholder = "Snooze all highlights for...",
            Options =
            {
                new LocalSelectionComponentOption("10 minutes", "10"),
                new LocalSelectionComponentOption("30 minutes", "30"),
                new LocalSelectionComponentOption("1 hour", "60"),
                new LocalSelectionComponentOption("8 hours", "480"),
                new LocalSelectionComponentOption("24 hours", "1440")
            }
        });
    }

    public DiscordBotBase Bot => (DiscordBotBase)Menu.Client;
 
    public async ValueTask BlacklistAuthorAsync(ButtonEventArgs e)
    {
        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);

        var globalUser = await db.GetOrCreateGlobalUserAsync(e.AuthorId);
        globalUser.BlacklistedHighlightUserIds.Add(_message.Author.Id);
        await db.SaveChangesAsync();
        
        Bot.Services.GetRequiredService<HighlightHandlingService>().InvalidateCache();

        e.Button.IsDisabled = true;
        e.Button.Label = $"{_message.Author.Tag} blacklisted.";
    }

    public async ValueTask BlacklistChannelAsync(ButtonEventArgs e)
    {
        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);

        var globalUser = await db.GetOrCreateGlobalUserAsync(e.AuthorId);
        globalUser.BlacklistedHighlightChannelIds.Add(_message.ChannelId);
        await db.SaveChangesAsync();
        
        Bot.Services.GetRequiredService<HighlightHandlingService>().InvalidateCache();

        /*
        await e.Interaction.Response().SendMessageAsync(new LocalInteractionMessageResponse()
            .WithContent($"Messages sent in {channel.Mention} will no longer highlight you.")
            .WithIsEphemeral());
        */

        var channelName = (_message.GetChannel() ?? Bot.GetChannel(_message.GuildId!.Value, _message.ChannelId)) is { } channel
            ? $"#{channel.Name}"
            : _message.ChannelId.ToString();
        e.Button.IsDisabled = true;
        e.Button.Label = $"{channelName} blacklisted.";
    }

    public async ValueTask DismissAsync(ButtonEventArgs? e = null)
    {
        ClearComponents();

        AddComponent(new LinkButtonViewComponent(_message.GetJumpUrl())
        {
            Label = "Jump to message",
            Position = 0
        });

        await Menu.ApplyChangesAsync(e);

        Menu.Stop();
    }

    public async ValueTask SnoozeAsync(SelectionEventArgs e)
    {
        var mentions = Bot.Services.GetRequiredService<SlashCommandMentionService>();
        var minutes = int.Parse(e.SelectedOptions[0].Value.Value);
        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var globalUser = await db.GetOrCreateGlobalUserAsync(e.AuthorId);
        var snoozedUntil = DateTimeOffset.UtcNow.AddMinutes(minutes);
        globalUser.HighlightsSnoozedUntil = snoozedUntil;
        await db.SaveChangesAsync();
        Bot.Services.GetRequiredService<HighlightHandlingService>().InvalidateCache();
        
        await e.Interaction.Response().SendMessageAsync(new LocalInteractionMessageResponse()
            .WithContent($"You've snoozed {Markdown.Underline("all")} highlights until " +
                         $"{Markdown.Timestamp(snoozedUntil, Markdown.TimestampFormat.LongDateTime)}.\n" +
                         $"(You can undo this with the command {mentions.GetMention("highlight snooze disable")}.)")
            .WithIsEphemeral());
        
        RemoveComponent(e.Selection);
    }
}
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Gateway;
using Disqord.Rest;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Administrator.Bot;

public class LoggingChannelConfigurationView : GuildConfigurationViewBase
{
    public const string SELECTION_TEXT = "Logging Channels";
    
    private readonly SelectionViewComponent _typeSelect;
    private readonly SelectionViewComponent _channelSelect;
    private ICollection<LoggingChannel> _existingChannels;
    private List<LogEventType> _typesToLog = new List<LogEventType>();

    public LoggingChannelConfigurationView(IDiscordApplicationGuildCommandContext context, ICollection<LoggingChannel> existingChannels) 
        : base(context)
    {
        _existingChannels = existingChannels;

        var options = GenerateTypeOptions();
        _typeSelect = new SelectionViewComponent(SelectTypesAsync)
        {
            Type = SelectionComponentType.String,
            Placeholder = "Select events to log.",
            MinimumSelectedOptions = 1,
            MaximumSelectedOptions = options.Count,
            Options = options
        };

        _channelSelect = new SelectionViewComponent(SelectChannelAsync)
        {
            ChannelTypes = new[] {ChannelType.Text},
            Type = SelectionComponentType.Channel,
            Placeholder = "Select the channel.",
            IsDisabled = true
        };

        AddComponent(_typeSelect);
        AddComponent(_channelSelect);
    }

    /*
    public override void FormatLocalMessage(LocalMessageBase message)
    {
        var embed = new LocalEmbed()
            .WithUnusualColor()
            .WithTitle("Logging Configuration");

        if (_typesToLog.Count > 0)
        {
            embed.AddField($"Pick a channel to log the following {"event".ToQuantity(_typesToLog.Count)}",
                _typesToLog.Select(x => Markdown.Bold(x.Humanize(LetterCasing.Sentence))));
        }

        base.FormatLocalMessage(message);
    }
    */
    
    protected override string FormatContent() => SELECTION_TEXT;

    public async ValueTask SelectTypesAsync(SelectionEventArgs e)
    {
        if (e.SelectedOptions.Any(x => x.Value.Value == "clear"))
        {
            await using var scope = _context.Bot.Services.CreateAsyncScopeWithDatabase(out var db);
            db.LoggingChannels.RemoveRange(_existingChannels);
            await db.SaveChangesAsync();
            
            _typesToLog.Clear();
            _existingChannels.Clear();

            await e.Interaction.Response().SendMessageAsync(new LocalInteractionMessageResponse()
                .WithContent("All logging channels have been cleared."));
            
            _typeSelect.Options = GenerateTypeOptions();
            return;
        }
        
        _typesToLog = e.SelectedOptions.Select(x => Enum.Parse<LogEventType>(x.Value.Value)).ToList();
        _channelSelect.IsDisabled = false;
        _typeSelect.Options = GenerateTypeOptions();
        ReportChanges();
    }

    public async ValueTask SelectChannelAsync(SelectionEventArgs e)
    {
        var channelId = e.SelectedEntities[0].Id;

        await using var scope = _context.Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        
        foreach (var eventType in _typesToLog)
        {
            if (await db.LoggingChannels.FindAsync(_context.GuildId.RawValue, eventType) is { } loggingChannel)
            {
                loggingChannel.ChannelId = channelId;
            }
            else
            {
                loggingChannel = new LoggingChannel(_context.GuildId, eventType, channelId);
                db.LoggingChannels.Add(loggingChannel);
            }
        }
        
        await db.SaveChangesAsync();
        await e.Interaction.Response().SendMessageAsync(new LocalInteractionMessageResponse()
            .WithContent(
                $"Success! The {string.Join(", ", _typesToLog.Select(x => Markdown.Bold(x.Humanize(LetterCasing.Sentence))))} " +
                $"{"event".ToQuantity(_typesToLog.Count, ShowQuantityAs.None)} will now be logged to {Mention.Channel(channelId)}."));

        _existingChannels = await db.LoggingChannels.Where(x => x.GuildId == _context.GuildId).ToListAsync();
        _typesToLog.Clear();
        _typeSelect.Options = GenerateTypeOptions();
        _channelSelect.IsDisabled = true;
        ReportChanges();
    }

    private IList<LocalSelectionComponentOption> GenerateTypeOptions()
    {
        var list = new List<LocalSelectionComponentOption>();
        var emojiService = _context.Bot.Services.GetRequiredService<EmojiService>();

        foreach (var value in typeof(LogEventType).GetEnumValues().Cast<LogEventType>())
        {
            var option = new LocalSelectionComponentOption()
                .WithLabel(value.Humanize(LetterCasing.Sentence))
                .WithValue(value.ToString());

            if (_typesToLog.Contains(value))
                option.WithIsDefault();

            if (_existingChannels.FirstOrDefault(x => x.EventType == value) is { } loggingChannel)
            {
                option.WithEmoji(emojiService.Names["white_check_mark"]);

                option.WithLabel(option.Label.Value + 
                    (_context.Bot.GetChannel(loggingChannel.GuildId, loggingChannel.ChannelId) is ITextChannel channel
                        ? $" - {channel.Tag}"
                        : $" - {loggingChannel.ChannelId}"));
            }

            list.Add(option);
        }
        
        list.Add(new LocalSelectionComponentOption().WithLabel("[CLEAR ALL]").WithValue("clear").WithEmoji(emojiService.Names["warning"]));

        return list;
    }
}
using Administrator.Core;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Qommon;
using Message = Administrator.Core.IMessageCacheService.Message;
using MessageCache = System.Collections.Concurrent.ConcurrentDictionary<Disqord.Snowflake, System.Collections.Concurrent.ConcurrentDictionary<Disqord.Snowflake, Administrator.Core.IMessageCacheService.Message>>;

namespace Administrator.Bot;

public sealed class MessageCacheService : DiscordBotService, IMessageCacheService
{
    public MessageCache Cache { get; } = new();

    protected override ValueTask OnMessageReceived(BotMessageReceivedEventArgs e)
    {
        if (e is { GuildId: not null, Message: IUserMessage m })
        {
            var message = Message.FromMessage(m);
            this.AddOrUpdateMessage(e.ChannelId, message);
        }

        return ValueTask.CompletedTask;
    }

    protected override ValueTask OnMessageUpdated(MessageUpdatedEventArgs e)
    {
        // we only care about content updates for this service
        var oldContent = e.OldMessage?.Content;
        var newContent = e.NewMessage?.Content ?? e.Model.Content.GetValueOrDefault();
        if (newContent is not null && oldContent != newContent && e.NewMessage?.Author is { } author)
        {
            var message = new Message(e.MessageId, author.Id, newContent, null);
            this.AddOrUpdateMessage(e.ChannelId, message);
        }

        return ValueTask.CompletedTask;
    }
}
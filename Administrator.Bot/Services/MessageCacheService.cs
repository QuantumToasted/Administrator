using Administrator.Core;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Humanizer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qommon;
using Qommon.Collections.ThreadSafe;

namespace Administrator.Bot;

public sealed class MessageCacheService(IOptions<AdministratorCacheConfiguration> config) : DiscordBotService
{
    private readonly TimeSpan _maxLifetime = TimeSpan.FromDays(config.Value.MaxMessageLifetimeDays);
    
    public IThreadSafeDictionary<Snowflake, IThreadSafeDictionary<Snowflake, Message>> Cache { get; } = CreateCache<Snowflake, IThreadSafeDictionary<Snowflake, Message>>();

    public Message? GetMessage(Snowflake channelId, Snowflake messageId)
        => Cache.TryGetValue(channelId, out var cache) && cache.TryGetValue(messageId, out var message) ? message : null;
    
    protected override ValueTask OnMessageReceived(BotMessageReceivedEventArgs e)
    {
        if (e.Message is not IUserMessage message)
            return ValueTask.CompletedTask;
        
        var cache = Cache.GetOrAdd(e.ChannelId, static _ => CreateCache<Snowflake, Message>());
        cache[e.MessageId] = new Message(message.Id, e.Message.Author, message.Content, message.Attachments);
        return ValueTask.CompletedTask;
    }

    protected override ValueTask OnMessageUpdated(MessageUpdatedEventArgs e)
    {
        var content = e.NewMessage?.Content ?? e.Model.Content.GetValueOrDefault();
        if (content is null) // only null - no content on the updated model
            return ValueTask.CompletedTask;
        
        if (e.NewMessage?.Author is not { } author)
        {
            return ValueTask.CompletedTask;
        }
        
        var cache = Cache.GetOrAdd(e.ChannelId, static _ => CreateCache<Snowflake, Message>());
        cache.AddOrUpdate(e.MessageId, new Message(e.MessageId, author, content, null), (_, msg) => msg with { Content = content });
        return ValueTask.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Bot.WaitUntilReadyAsync(stoppingToken);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            var cleared = 0;
            var now = DateTimeOffset.UtcNow;
            
            foreach (var cache in Cache.Values)
            {
                foreach (var id in cache.Keys)
                {
                    if (now - id.CreatedAt > _maxLifetime)
                    {
                        cache.Remove(id);
                        cleared++;
                    }
                }
            }

            if (cleared > 0)
            {
                Logger.LogInformation("Cleared {Count} stale messages over {Lifetime}.", cleared, _maxLifetime.Humanize());
            }
        }
    }

    private static ThreadSafeDictionary<TKey, TValue> CreateCache<TKey, TValue>() where TKey : notnull
        => ThreadSafeDictionary.Monitor.Create<TKey, TValue>();

    public sealed record Message(Snowflake Id, IUser Author, string Content, IReadOnlyList<IAttachment>? Attachments)
    {
        public static Message? FromMessage(IMessage? message)
        {
            if (message is null)
                return null;

            return new(message.Id, message.Author, message.Content, (message as IUserMessage)?.Attachments);
        }
    }
}
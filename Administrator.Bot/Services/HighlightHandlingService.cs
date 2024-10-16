using System.Collections.Concurrent;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Gateway;
using Disqord.Http;
using Disqord.Rest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Administrator.Bot;

public sealed class HighlightHandlingService(IMemoryCache cache) : DiscordBotService
{
    private const string CACHE_KEY = "Highlights";
    
    private readonly ConcurrentDictionary<Snowflake, HashSet<Snowflake>> _previouslyHighlightedUsers = new();

    public void InvalidateCache()
        => cache.Remove(CACHE_KEY);

    protected override ValueTask OnMessageReceived(BotMessageReceivedEventArgs e)
    {
        if (e.Message is not IGatewayUserMessage { Author.IsBot: false } message || string.IsNullOrWhiteSpace(message.Content) ||
            e.GuildId is not { } guildId || (e.Channel ?? Bot.GetChannel(guildId, e.ChannelId)) is not IMessageGuildChannel channel)
        {
            return ValueTask.CompletedTask;
        }

        return HighlightAsync(message, channel);
    }

    protected override ValueTask OnMessageUpdated(MessageUpdatedEventArgs e)
    {
        if (e.NewMessage is not IGatewayUserMessage { Author.IsBot: false } message || string.IsNullOrWhiteSpace(message.Content) ||
            e.GuildId is not { } guildId || Bot.GetChannel(guildId, e.ChannelId) is not IMessageGuildChannel channel)
        {
            return ValueTask.CompletedTask;
        }

        return HighlightAsync(message, channel);
    }

    protected override ValueTask OnMessageDeleted(MessageDeletedEventArgs e)
    {
        RemovePreviouslyHighlightedUsers(e.MessageId);
        return ValueTask.CompletedTask;
    }

    protected override ValueTask OnMessagesDeleted(MessagesDeletedEventArgs e)
    {
        foreach (var messageId in e.MessageIds)
        {
            RemovePreviouslyHighlightedUsers(messageId);
        }
        
        return ValueTask.CompletedTask;
    }

    private void RemovePreviouslyHighlightedUsers(Snowflake messageId)
        => _previouslyHighlightedUsers.Remove(messageId, out _);

    private async ValueTask HighlightAsync(IGatewayUserMessage message, IMessageGuildChannel channel)
    {
        //await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);

        if (message.Flags.HasFlag(MessageFlags.SuppressedNotifications))
            return;

        var allHighlights = await GetHighlightsAsync();
        var highlights = allHighlights.Where(x => !x.GuildId.HasValue || x.GuildId == channel.GuildId).ToList();
        
        /*
        var highlights = await db.Highlights
            .Include(x => x.Author)
            .Where(x => !x.GuildId.HasValue || x.GuildId == channel.GuildId)
            .ToListAsync();
        */

        if (highlights.Count == 0)
            return;

        var guild = Bot.GetGuild(channel.GuildId)!;
        var author = message.Author as IMember;
        
        var now = message.CreatedAt();
        var memberCache = new Dictionary<(Snowflake GuildId, Snowflake MemberId), IMember>();
        var userCache = new Dictionary<Snowflake, IUser>();
        var recentMessages = Bot.CacheProvider.TryGetMessages(channel.Id, out var messages)
            ? messages.Values.OrderByDescending(x => x.Id).ToList()
            : new List<CachedUserMessage>();

        foreach (var highlight in highlights.Where(x => x.IsMatch(message)).DistinctBy(x => x.AuthorId))
        {
            if (highlight.Author?.HighlightsSnoozedUntil > now)
                continue; // Don't highlight users who have snoozed them

            if (highlight.Author!.BlacklistedHighlightChannelIds.Contains(channel.Id) ||
                highlight.Author.BlacklistedHighlightUserIds.Contains(message.Author.Id))
            {
                continue; // don't highlight from blacklisted users/channels
            }

            var filteredMessages = recentMessages
                .Where(x => now - x.CreatedAt() < highlight.Author!.ResumeHighlightsAfterInterval)
                .Take(highlight.Author!.ResumeHighlightsAfterMessageCount)
                .ToList();
            
            if (highlight.AuthorId == message.Author.Id || // Don't highlight the person who sent this message
                highlight.GuildId?.Equals(channel.GuildId) == false || // Don't highlight them if this is a different guild (and not a global highlight)
                filteredMessages.Any(x => x.Author.Id == highlight.AuthorId)) // Don't highlight them if any recent messages were sent by them
            {
                continue;
            }

            if (!memberCache.TryGetValue((channel.GuildId, highlight.AuthorId), out var member))
            {
                member = await Bot.GetOrFetchMemberAsync(channel.GuildId, highlight.AuthorId);
                if (member is null)
                    continue;

                memberCache[(channel.GuildId, highlight.AuthorId)] = member;
            }

            if (!member.CalculateChannelPermissions(channel).HasFlag(Permissions.ViewChannels))
            {
                continue;
            }

            if (!userCache.TryGetValue(highlight.AuthorId, out var user))
            {
                user = await Bot.GetOrFetchUserAsync(highlight.AuthorId);
                if (user is null)
                    continue;

                userCache[highlight.AuthorId] = user;
            }
            
            var previouslyHighlightedUsers = _previouslyHighlightedUsers.GetOrAdd(message.Id, static _ => new HashSet<Snowflake>());
            if (!previouslyHighlightedUsers.Add(highlight.AuthorId))
                continue; // already highlighted by this message

            try
            {
                var dmChannel = await Bot.CreateDirectChannelAsync(highlight.AuthorId);

                var view = new InitialHighlightView(message, channel, x =>
                {
                    x.WithContent($"You've been highlighted in {guild.Name} by {author!.GetDisplayName()}!")
                        .AddEmbed(new LocalEmbed()
                            .WithUnusualColor()
                            .WithDescription(message.Content)
                            .WithAuthor($"{author!.GetDisplayName()} - in {channel.Tag}", author!.GetGuildAvatarUrl())
                            .WithFooter($"Highlighted text: {highlight.Text}")
                            .WithTimestamp(message.CreatedAt()));

                    if (message.Attachments.FirstOrDefault(y => new Uri(y.Url).HasImageExtension()) is { } image)
                    {
                        x.Embeds.Value[0].WithImageUrl(image.Url);
                    }
                });

                await Bot.StartMenuAsync(dmChannel.Id, new AdminTextMenu(view) { ClearComponents = false, AuthorId = highlight.AuthorId },
                    TimeSpan.FromHours(12));
            }
            catch (InvalidOperationException ex) when (ex.InnerException is RestApiException { StatusCode: HttpResponseStatusCode.Forbidden })
            { }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An exception occurred attempting to highlight user {UserId}.",
                    highlight.AuthorId.RawValue);
            }
        }
    }

    private async Task<List<Highlight>> GetHighlightsAsync()
    {
        if (cache.TryGetValue<List<Highlight>>(CACHE_KEY, out var cachedHighlights))
            return cachedHighlights!;

        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var dbHighlights = await db.Highlights
            .AsNoTracking()
            .Include(x => x.Author)
            .ToListAsync();

        return cache.Set(CACHE_KEY, dbHighlights);
    }
}
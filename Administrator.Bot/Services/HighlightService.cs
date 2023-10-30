using System.Collections.Concurrent;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Gateway;
using Disqord.Http;
using Disqord.Rest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Administrator.Bot;

public sealed class HighlightService : DiscordBotService
{
    private readonly ConcurrentDictionary<Snowflake, HashSet<Snowflake>> _previouslyHighlightedUsers = new();
    
    protected override async ValueTask OnMessageReceived(BotMessageReceivedEventArgs e)
    {
        if (e.Message is not IGatewayUserMessage message || string.IsNullOrWhiteSpace(message.Content) || e.GuildId is not { } guildId)
            return;
        
        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var highlights = await db.Highlights
            .Include(x => x.Author)
            .Where(x => !x.GuildId.HasValue || x.GuildId == guildId)
            .ToListAsync();

        if (highlights.Count == 0)
            return;

        var guild = Bot.GetGuild(guildId)!;
        var channel = e.Channel ?? Bot.GetChannel(guildId, e.ChannelId) as IMessageGuildChannel;
        var author = message.Author as IMember;
        
        var now = e.Message.CreatedAt();
        var memberCache = new Dictionary<(Snowflake GuildId, Snowflake MemberId), IMember>();
        var userCache = new Dictionary<Snowflake, IUser>();
        var recentMessages = Bot.CacheProvider.TryGetMessages(e.ChannelId, out var messages)
            ? messages.Values.OrderByDescending(x => x.Id).ToList()
            : new List<CachedUserMessage>();

        foreach (var highlight in highlights.Where(x => x.IsMatch(e.Message)).DistinctBy(x => x.AuthorId))
        {
            if (highlight.Author?.HighlightsSnoozedUntil > now)
                continue; // don't highlight users who have snoozed them

            var filteredMessages = recentMessages
                .Where(x => now - x.CreatedAt() < highlight.Author!.ResumeHighlightsAfterInterval)
                .Take(highlight.Author!.ResumeHighlightsAfterMessageCount)
                .ToList();
            
            if (highlight.AuthorId == message.Author.Id || // Don't highlight the person who sent this message
                highlight.GuildId?.Equals(guildId) == false || // Don't highlight them if this is a different guild (and not a global highlight)
                filteredMessages.Any(x => x.Author.Id == highlight.AuthorId)) // Don't highlight them if any recent messages were sent by them
            {
                continue;
            }

            if (!memberCache.TryGetValue((guildId, highlight.AuthorId), out var member))
            {
                member = await Bot.GetOrFetchMemberAsync(guildId, highlight.AuthorId);
                if (member is null)
                    continue;

                memberCache[(guildId, highlight.AuthorId)] = member;
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

                await Bot.StartMenuAsync(dmChannel.Id, new AdminTextMenu(view), TimeSpan.FromHours(12));
            }
            catch (RestApiException ex) when (ex.StatusCode == HttpResponseStatusCode.Forbidden)
            { }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An exception occurred attempting to highlight user {UserId}.",
                    highlight.AuthorId.RawValue);
            }
        }
    }

    private async ValueTask HighlightAsync(IGatewayUserMessage message, IMessageGuildChannel channel)
    {
        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var highlights = await db.Highlights
            .Include(x => x.Author)
            .Where(x => !x.GuildId.HasValue || x.GuildId == channel.GuildId)
            .ToListAsync();

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

                await Bot.StartMenuAsync(dmChannel.Id, new AdminTextMenu(view), TimeSpan.FromHours(12));
            }
            catch (RestApiException ex) when (ex.StatusCode == HttpResponseStatusCode.Forbidden)
            { }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An exception occurred attempting to highlight user {UserId}.",
                    highlight.AuthorId.RawValue);
            }
        }
    }
}
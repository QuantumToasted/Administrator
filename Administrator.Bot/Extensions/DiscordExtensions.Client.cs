using Disqord;
using Disqord.Gateway;
using Disqord.Http;
using Disqord.Rest;
using Microsoft.Extensions.Logging;
using Qommon;

namespace Administrator.Bot;

public static partial class DiscordExtensions
{
    public static async ValueTask<IUser?> GetOrFetchUserAsync(this DiscordClientBase client, Snowflake userId)
    {
        if (client.GetUser(userId) is { } cachedUser)
            return cachedUser;

        return await client.FetchUserAsync(userId);
    }

    public static async ValueTask<IMember?> GetOrFetchMemberAsync(this DiscordClientBase client, Snowflake guildId, Snowflake memberId)
    {
        if (client.GetMember(guildId, memberId) is { } cachedMember)
            return cachedMember;

        // borrowed from DQ's MemberTypeParser
        if (client.ApiClient.GetShard(guildId)?.RateLimiter.GetRemainingRequests() < 3)
        {
            return await client.FetchMemberAsync(guildId, memberId);
        }

        var members = await client.Chunker.QueryAsync(guildId, new[] { memberId });
        return members.GetValueOrDefault(memberId);
    }

    public static async Task<IUserMessage?> TrySendMessageAsync(this DiscordClientBase client, Snowflake channelId, LocalMessage message)
    {
        try
        {
            return await client.SendMessageAsync(channelId, message);
        }
        catch (RestApiException ex) when (ex.HttpResponse.StatusCode is HttpResponseStatusCode.Forbidden or HttpResponseStatusCode.NotFound || ex.ErrorModel?.Code == RestApiErrorCode.CannotSendMessagesToThisUser)
        {
            return null;
        }
        catch (Exception ex)
        {
            client.Logger.LogError(ex, "TrySendMessage failed without a permissible error.");
            throw;
        }
    }

    public static async Task<IUserMessage?> TrySendDirectMessageAsync(this DiscordClientBase client, Snowflake userId, LocalMessage message)
    {
        IDirectChannel dmChannel;

        try
        {
            dmChannel = await client.CreateDirectChannelAsync(userId);
        }
        catch (RestApiException ex) when (ex.StatusCode == HttpResponseStatusCode.BadRequest) // should ONLY be thrown for bots
        {
            return null;
        }

        return await client.TrySendMessageAsync(dmChannel.Id, message);
    }

    public static async Task<IUserMessage?> TryModifyMessageToAsync(this DiscordClientBase client, Snowflake channelId, Snowflake messageId, LocalMessageBase message)
    {
        try
        {
            if (message.Attachments.TryGetValue(out var attachments))
            {
                for (var i = 0; i < attachments.Count; i++)
                {
                    // editing a message's attachments requires them to have an ID, apparently?
                    attachments[i].WithId(messageId + (ulong) i + 1);
                }
            }

            var attachments2 = message.Attachments.GetValueOrDefault()?.ToList() ?? new List<LocalAttachment>();

            return await client.ModifyMessageAsync(channelId, messageId, x =>
            {
                x.Content = message.Content;
                x.Embeds = message.Embeds.GetValueOrDefault()?.ToList() ?? Optional<IEnumerable<LocalEmbed>>.Empty;
                x.AllowedMentions = message.AllowedMentions;
                //x.Attachments = message.Attachments.GetValueOrDefault()?.ToList() ?? new List<LocalAttachment>(); // Remove attachments, I guess
                x.Attachments = attachments2;
                x.Components = message.Components.GetValueOrDefault()?.ToList() ?? Optional<IEnumerable<LocalRowComponent>>.Empty;
                x.Flags = message.Flags;
                x.StickerIds = message.StickerIds.GetValueOrDefault()?.ToList() ?? Optional<IEnumerable<Snowflake>>.Empty;
            });
        }
        catch (RestApiException ex) when (ex.HttpResponse.StatusCode is HttpResponseStatusCode.Forbidden or HttpResponseStatusCode.NotFound || ex.ErrorModel?.Code == RestApiErrorCode.CannotSendMessagesToThisUser)
        {
            return null;
        }
        catch (Exception ex)
        {
            client.Logger.LogError(ex, "TryModifyMessageTo failed without a permissible error.");
            throw;
        }
    }
    
    public static bool HasPermissionsInGuild(this DiscordClientBase client, Snowflake guildId, Permissions permissions)
    {
        var member = client.GetMember(guildId, client.CurrentUser.Id);
        return (member!.CalculateGuildPermissions() & permissions) == permissions;
    }

    public static async Task AddReactionsAsync(this DiscordClientBase client, Snowflake channelId, Snowflake messageId, params LocalEmoji[] emoji)
    {
        Guard.IsNotEmpty(emoji);
        
        foreach (var e in emoji)
        {
            await client.AddReactionAsync(channelId, messageId, e);
        }
    }
    
    public static async Task AddReactionsAsync(this DiscordClientBase client, Snowflake channelId, Snowflake messageId, params string[] unicodeEmoji)
    {
        Guard.IsNotEmpty(unicodeEmoji);
        
        foreach (var unicode in unicodeEmoji)
        {
            await client.AddReactionAsync(channelId, messageId, LocalEmoji.Unicode(unicode));
        }
    }
}
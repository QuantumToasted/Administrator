using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Rest;
using Humanizer;
using Qmmands;

namespace Administrator.Bot;

public sealed partial class PruneModule(EmojiService emojis) : DiscordApplicationGuildModuleBase
{
    public partial Task<IResult> PruneAll(int limit, IChannel? channel, Snowflake? startFromMessageId, FetchDirection? direction)
        => Prune(limit, channel?.Id, startFromMessageId, direction, null);

    public partial Task<IResult> PruneUser(int limit, IUser user, IChannel? channel, Snowflake? startFromMessageId, FetchDirection? direction) 
        => Prune(limit, channel?.Id, startFromMessageId, direction, m => m.Author.Id == user.Id);

    public partial Task<IResult> PruneBots(int limit, IChannel? channel, Snowflake? startFromMessageId, FetchDirection? direction) 
        => Prune(limit, channel?.Id, startFromMessageId, direction, x => x.Author.IsBot);

    public partial Task<IResult> PruneContent(int limit, string text, IChannel? channel, Snowflake? startFromMessageId, FetchDirection? direction)
        => Prune(limit, channel?.Id, startFromMessageId, direction, m => m.Content.Contains(text, StringComparison.InvariantCultureIgnoreCase));

    public partial Task<IResult> PruneStickers(int limit, IChannel? channel, Snowflake? startFromMessageId, FetchDirection? direction) 
        => Prune(limit, channel?.Id, startFromMessageId, direction, m => (m as IUserMessage)?.Stickers.Count > 0);

    public partial Task<IResult> PruneAttachments(int limit, IChannel? channel, Snowflake? startFromMessageId, FetchDirection? direction) 
        => Prune(limit, channel?.Id, startFromMessageId, direction, m => (m as IUserMessage)?.Attachments.Count > 0 || (m as IUserMessage)?.Embeds.Count(x => x.Type is "image") > 0);

    public partial Task<IResult> PruneEmojis(int limit, IChannel? channel, Snowflake? startFromMessageId, FetchDirection? direction)
    {
        return Prune(limit, channel?.Id, startFromMessageId, direction, m =>
        {
            if (string.IsNullOrWhiteSpace(m.Content))
                return false;

            return EmojiService.CustomEmojiRegex.IsMatch(m.Content) ||
                   emojis.Surrogates.Keys.Any(x => m.Content.Contains(x)); // This is REALLY expensive
        });
    }

    private async Task<IResult> Prune(int limit, Snowflake? channelId, Snowflake? startFromMessageId, FetchDirection? direction, Func<IMessage, bool>? filterFunc)
    {
        if (direction.HasValue && !startFromMessageId.HasValue)
            return Response($"Specifying {Markdown.Code("direction")} explicitly requires specifying {Markdown.Code("start-from-message-id")}.").AsEphemeral();

        direction ??= FetchDirection.Before;
        
        await Deferral();

        var deferralMessage = await Context.Interaction.Followup().FetchResponseAsync();

        channelId ??= Context.ChannelId;

        var messagesToDelete = new List<IMessage>();
        var emptyCount = 0;

        await foreach (var chunk in Bot.EnumerateMessages(channelId.Value, int.MaxValue, direction.Value, startFromMessageId))
        {
            var currentCount = messagesToDelete.Count;

            var orderedChunk = direction == FetchDirection.Before
                ? chunk.OrderByDescending(x => x.Id)
                : chunk.OrderBy(x => x.Id);

            foreach (var message in orderedChunk)
            {
                if (message.Id == deferralMessage.Id)
                    continue; // ignore the deferral message

                if (messagesToDelete.Count == limit)
                    break;

                if (filterFunc is null || filterFunc.Invoke(message))
                    messagesToDelete.Add(message);
            }

            if (currentCount == messagesToDelete.Count)
                emptyCount++;

            if (messagesToDelete.Count == limit || emptyCount == 5)
                break;
        }
        
        if (!direction.HasValue && startFromMessageId.HasValue && messagesToDelete.All(x => x.Id != startFromMessageId.Value))
        {
            try
            {
                if (await Bot.FetchMessageAsync(channelId.Value, startFromMessageId.Value) is { } startMessage)
                    messagesToDelete.Add(startMessage);
            }
            catch (RestApiException)
            { }
        }

        messagesToDelete = messagesToDelete
            .Where(x => DateTimeOffset.UtcNow - x.CreatedAt() < TimeSpan.FromDays(14))
            .ToList();

        if (messagesToDelete.Count == 0)
            return Response("No messages were deleted. There may have been no messages to delete, or they may have all been older than 2 weeks.");

        await Bot.DeleteMessagesAsync(channelId.Value, messagesToDelete.Select(x => x.Id),
            new DefaultRestRequestOptions().WithReason($"/prune command used by {Context.AuthorId}"));

        return Response($"{"message".ToQuantity(messagesToDelete.Count)} deleted.");
    }
}
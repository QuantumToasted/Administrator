using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Rest;
using Humanizer;
using Qmmands;
using IResult = Qmmands.IResult;

namespace Administrator.Bot;

[SlashGroup("prune")]
[RequireInitialAuthorPermissions(Permissions.ManageMessages)]
[RequireBotPermissions(Permissions.ManageMessages)]
public sealed class PruneModule(EmojiService emojis) : DiscordApplicationGuildModuleBase
{
    [SlashCommand("all")]
    [Description("Prunes all messages in a channel.")]
    public Task<IResult> PruneAllAsync(
        [Description("The maximum amount of messages to prune.")]
        [Range(1, 1000)]
            int limit,
        [Description("The channel to prune messages in. Defaults to the current channel.")]
        [ChannelTypes(ChannelType.Text, ChannelType.PublicThread, ChannelType.PrivateThread)]
        [AuthorCanViewChannel]
        [RequireBotChannelPermissions(Permissions.ViewChannels | Permissions.ManageMessages)]
            IChannel? channel = null,
        [Description("Start from this message's ID.")]
            Snowflake? startFromMessageId = null,
        [Description("The direction to grab messages for deletion. Affected by start-from-message-id. Default: Before")]
        [Choice("Before start-from-message-id", FetchDirection.Before)]
        [Choice("After start-from-message-id", FetchDirection.After)]
            FetchDirection? direction = null)
    {
        return PruneAsync(limit, channel?.Id, startFromMessageId, direction, null);
    }

    [SlashCommand("user")]
    [Description("Prunes all messages from a user or member in a channel.")]
    public Task<IResult> PruneUserAsync(
        [Description("The maximum amount of messages to prune.")]
        [Range(1, 1000)]
            int limit,
        [Description("Only this user/member's messages will be pruned.")]
            IUser user,
        [Description("The channel to prune messages in. Defaults to the current channel.")]
        [ChannelTypes(ChannelType.Text, ChannelType.PublicThread, ChannelType.PrivateThread)]
        [AuthorCanViewChannel]
        [RequireBotChannelPermissions(Permissions.ViewChannels | Permissions.ManageMessages)]
            IChannel? channel = null,
        [Description("Start from this message's ID.")]
            Snowflake? startFromMessageId = null,
        [Description("The direction to grab messages for deletion. Affected by start-from-message-id.")]
        [Choice("Before start-from-message-id", FetchDirection.Before)]
        [Choice("After start-from-message-id", FetchDirection.After)]
            FetchDirection? direction = null)
    {
        return PruneAsync(limit, channel?.Id, startFromMessageId, direction, m => m.Author.Id == user.Id);
    }

    [SlashCommand("bot")]
    [Description("Prunes all messages from bots in a channel.")]
    public Task<IResult> PruneBotsAsync(
        [Description("The maximum amount of messages to prune.")]
        [Range(1, 1000)]
            int limit,
        [Description("The channel to prune messages in. Defaults to the current channel.")]
        [ChannelTypes(ChannelType.Text, ChannelType.PublicThread, ChannelType.PrivateThread)]
        [AuthorCanViewChannel]
        [RequireBotChannelPermissions(Permissions.ViewChannels | Permissions.ManageMessages)]
            IChannel? channel = null,
        [Description("Start from this message's ID.")]
            Snowflake? startFromMessageId = null,
        [Description("The direction to grab messages for deletion. Affected by start-from-message-id.")]
        [Choice("Before start-from-message-id", FetchDirection.Before)]
        [Choice("After start-from-message-id", FetchDirection.After)]
            FetchDirection? direction = null)
    {
        return PruneAsync(limit, channel?.Id, startFromMessageId, direction, x => x.Author.IsBot);
    }

    [SlashCommand("text")]
    [Description("Prunes all messages containing specific text content in a channel.")]
    public Task<IResult> PruneContentAsync(
        [Description("The maximum amount of messages to prune.")]
        [Range(1, 1000)]
            int limit,
        [Description("Only messages containing this text will be pruned. Case-insensitive.")]
            string text,
        [Description("The channel to prune messages in. Defaults to the current channel.")]
        [ChannelTypes(ChannelType.Text, ChannelType.PublicThread, ChannelType.PrivateThread)]
        [AuthorCanViewChannel]
        [RequireBotChannelPermissions(Permissions.ViewChannels | Permissions.ManageMessages)]
            IChannel? channel = null,
        [Description("Start from this message's ID.")]
            Snowflake? startFromMessageId = null,
        [Description("The direction to grab messages for deletion. Affected by start-from-message-id.")]
        [Choice("Before start-from-message-id", FetchDirection.Before)]
        [Choice("After start-from-message-id", FetchDirection.After)]
            FetchDirection? direction = null)
    {
        return PruneAsync(limit, channel?.Id, startFromMessageId, direction, m => m.Content.Contains(text, StringComparison.InvariantCultureIgnoreCase));
    }

    [SlashCommand("stickers")]
    [Description("Prunes all messages containing stickers in a channel.")]
    public Task<IResult> PruneStickersAsync(
        [Description("The maximum amount of messages to prune.")]
        [Range(1, 1000)]
            int limit,
        [Description("The channel to prune messages in. Defaults to the current channel.")]
        [ChannelTypes(ChannelType.Text, ChannelType.PublicThread, ChannelType.PrivateThread)]
        [AuthorCanViewChannel]
        [RequireBotChannelPermissions(Permissions.ViewChannels | Permissions.ManageMessages)]
            IChannel? channel = null,
        [Description("Start from this message's ID.")]
            Snowflake? startFromMessageId = null,
        [Description("The direction to grab messages for deletion. Affected by start-from-message-id.")]
        [Choice("Before start-from-message-id", FetchDirection.Before)]
        [Choice("After start-from-message-id", FetchDirection.After)]
            FetchDirection? direction = null)
    {
        return PruneAsync(limit, channel?.Id, startFromMessageId, direction, m => (m as IUserMessage)?.Stickers.Count > 0);
    }

    [SlashCommand("attachments")]
    [Description("Prunes all messages containing attachments (like images) in a channel.")]
    public Task<IResult> PruneAttachmentsAsync(
        [Description("The maximum amount of messages to prune.")]
        [Range(1, 1000)]
            int limit,
        [Description("The channel to prune messages in. Defaults to the current channel.")]
        [ChannelTypes(ChannelType.Text, ChannelType.PublicThread, ChannelType.PrivateThread)]
        [AuthorCanViewChannel]
        [RequireBotChannelPermissions(Permissions.ViewChannels | Permissions.ManageMessages)]
            IChannel? channel = null,
        [Description("Start from this message's ID.")]
            Snowflake? startFromMessageId = null,
        [Description("The direction to grab messages for deletion. Affected by start-from-message-id.")]
        [Choice("Before start-from-message-id", FetchDirection.Before)]
        [Choice("After start-from-message-id", FetchDirection.After)]
            FetchDirection? direction = null)
    {
        return PruneAsync(limit, channel?.Id, startFromMessageId, direction, 
            m => (m as IUserMessage)?.Attachments.Count > 0 || (m as IUserMessage)?.Embeds.Count(x => x.Type is "image") > 0);
    }

    [SlashCommand("emoji")]
    [Description("Prunes all messages containing emojis in a channel.")]
    public Task<IResult> PruneEmojisAsync(
        [Description("The maximum amount of messages to prune.")]
        [Range(1, 1000)]
            int limit,
        [Description("The channel to prune messages in. Defaults to the current channel.")]
        [ChannelTypes(ChannelType.Text, ChannelType.PublicThread, ChannelType.PrivateThread)]
        [AuthorCanViewChannel]
        [RequireBotChannelPermissions(Permissions.ViewChannels | Permissions.ManageMessages)]
            IChannel? channel = null,
        [Description("Start from this message's ID.")]
            Snowflake? startFromMessageId = null,
        [Description("The direction to grab messages for deletion. Affected by start-from-message-id.")]
        [Choice("Before start-from-message-id", FetchDirection.Before)]
        [Choice("After start-from-message-id", FetchDirection.After)]
            FetchDirection? direction = null)
    {
        return PruneAsync(limit, channel?.Id, startFromMessageId, direction, m =>
        {
            if (string.IsNullOrWhiteSpace(m.Content))
                return false;

            return EmojiService.CustomEmojiRegex.IsMatch(m.Content) ||
                   emojis.Surrogates.Keys.Any(x => m.Content.Contains(x)); // This is REALLY expensive
        });
    }

    private async Task<IResult> PruneAsync(int limit, Snowflake? channelId, Snowflake? startFromMessageId, FetchDirection? direction, Func<IMessage, bool>? filterFunc)
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
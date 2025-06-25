using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Rest;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("prune")]
[RequireInitialAuthorPermissions(Permissions.ManageMessages)]
[RequireBotPermissions(Permissions.ManageMessages)]
public sealed partial class PruneModule
{
    [SlashCommand("all")]
    [Description("Prunes all messages in a channel.")]
    public partial Task<IResult> PruneAll(
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
            FetchDirection? direction = null);

    [SlashCommand("user")]
    [Description("Prunes all messages from a user or member in a channel.")]
    public partial Task<IResult> PruneUser(
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
            FetchDirection? direction = null);

    [SlashCommand("bot")]
    [Description("Prunes all messages from bots in a channel.")]
    public partial Task<IResult> PruneBots(
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
            FetchDirection? direction = null);

    [SlashCommand("text")]
    [Description("Prunes all messages containing specific text content in a channel.")]
    public partial Task<IResult> PruneContent(
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
            FetchDirection? direction = null);

    [SlashCommand("stickers")]
    [Description("Prunes all messages containing stickers in a channel.")]
    public partial Task<IResult> PruneStickers(
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
            FetchDirection? direction = null);

    [SlashCommand("attachments")]
    [Description("Prunes all messages containing attachments (like images) in a channel.")]
    public partial Task<IResult> PruneAttachments(
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
            FetchDirection? direction = null);

    [SlashCommand("emoji")]
    [Description("Prunes all messages containing emojis in a channel.")]
    public partial Task<IResult> PruneEmojis(
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
            FetchDirection? direction = null);
}
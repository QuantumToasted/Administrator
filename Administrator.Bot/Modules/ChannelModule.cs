using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("channel")]
[RequireInitialAuthorPermissions(Permissions.ManageChannels)]
[RequireBotPermissions(Permissions.ManageChannels)]
public sealed partial class ChannelModule
{
    [SlashCommand("info")]
    [Description("Displays information about a channel.")]
    public partial IResult DisplayInfo(
        [Description("The channel to display information for.")]
        [AuthorCanViewChannel]
        [BotCanViewChannel]
            IChannel channel);

    [SlashGroup("create")]
    public sealed partial class ChannelCreateModule
    {
        [SlashCommand("text")]
        [Description("Creates a new text channel.")]
        public partial Task CreateTextChannel();

        [SlashCommand("voice")]
        [Description("Creates a new voice channel.")]
        public partial Task CreateVoiceChannel();

        [SlashCommand("category")]
        [Description("Creates a new category channel.")]
        public partial Task CreateCategoryChannel();
    }

    [SlashCommand("clone")]
    [Description("Clones an existing channel.")]
    public partial Task<IResult> Clone(
        [Description("The channel to clone.")]
        [ChannelTypes(ChannelType.Forum, ChannelType.Text, ChannelType.Voice, ChannelType.Category)]
        [AuthorCanViewChannel]
        [BotCanViewChannel]
            IChannel channel,
        [Description("The name of the new channel. Defaults to the original channel's name.")]
            string? name = null);

    [SlashCommand("modify")]
    [Description("Modifies an existing channel.")]
    public partial Task<IResult> Modify(
        [Description("The channel to modify.")]
        [ChannelTypes(ChannelType.Forum, ChannelType.Text, ChannelType.Voice, ChannelType.Category)]
        [AuthorCanViewChannel]
        [BotCanViewChannel]
            IChannel channel);

    [SlashCommand("move")]
    [Description("Moves a channel above or below another channel.")]
    public partial Task<IResult> Move(
        [Description("The channel to move above/below target-channel.")]
        [ChannelTypes(ChannelType.Forum, ChannelType.Text, ChannelType.Voice, ChannelType.Category)]
        [AuthorCanViewChannel]
        [BotCanViewChannel]
            IChannel channelToMove,
        [Description("Whether to move channel-to-move above or below target-channel.")]
            MoveDirection direction,
        [Description("The channel to move channel-to-move above/below.")]
        [ChannelTypes(ChannelType.Forum, ChannelType.Text, ChannelType.Voice, ChannelType.Category)]
        [AuthorCanViewChannel]
        [BotCanViewChannel]
            IChannel targetChannel);

    [SlashCommand("delete")]
    [Description("Deletes an existing channel permanently.")]
    public partial Task Delete(
        [Description("The channel to delete.")]
        [ChannelTypes(ChannelType.Forum, ChannelType.Text, ChannelType.Voice, ChannelType.Category)]
        [AuthorCanViewChannel]
        [BotCanViewChannel]
            IChannel channel);

    [SlashCommand("slowmode")]
    [Description("Modifies a channel's slowmode (how often users can send messages).")]
    public partial Task<IResult> ModifySlowmode(
        [Description("The channel having its slowmode modified. Defaults to the current channel.")]
        [ChannelTypes(ChannelType.Text, ChannelType.Voice, ChannelType.PrivateThread, ChannelType.PublicThread)]
        [AuthorCanViewChannel]
        [BotCanViewChannel]
            IChannel? channel = null,
        [Description("The duration between messages. Supply nothing to disable slowmode.")]
            TimeSpan? duration = null);
}
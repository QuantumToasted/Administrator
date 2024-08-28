using Disqord;
using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("message")]
[RequireInitialAuthorPermissions(Permissions.ManageMessages)]
public sealed partial class MessageModule
{
    [SlashCommand("send")]
    [Description("Sends a message as the bot.")]
    public partial IResult Send(
        [Description("The channel to send the message to. Defaults to the current channel you are in.")]
        [ChannelTypes(ChannelType.Text, ChannelType.PrivateThread, ChannelType.PublicThread)]
        [RequireAuthorChannelPermissions(Permissions.SendMessages)]
        [RequireBotChannelPermissions(Permissions.SendMessages)]
            IChannel? channel = null,
        [Description("The text to send.")]
            string? text = null);

    [SlashCommand("modify")]
    [Description("Modifies a message that was sent by the bot.")]
    public partial Task<IResult> Modify(
        [Description("The channel the message was sent in.")]
        [ChannelTypes(ChannelType.Text, ChannelType.PrivateThread, ChannelType.PublicThread)]
        [RequireAuthorChannelPermissions(Permissions.SendMessages)]
        [RequireBotChannelPermissions(Permissions.SendMessages)]
            IChannel channel,
        [Description("The ID of the message.")]
            Snowflake messageId);
}
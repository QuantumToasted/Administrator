using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Rest;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("message")]
[RequireInitialAuthorPermissions(Permissions.ManageMessages)]
public sealed class MessageModule : DiscordApplicationGuildModuleBase
{
    [SlashCommand("send")]
    [Description("Sends a message as the bot.")]
    public IResult Send(
        [Description("The channel to send the message to. Defaults to the current channel you are in.")]
        [ChannelTypes(ChannelType.Text, ChannelType.PrivateThread, ChannelType.PublicThread)]
        [RequireAuthorChannelPermissions(Permissions.SendMessages)]
        [RequireBotChannelPermissions(Permissions.SendMessages)]
            IChannel? channel = null,
        [Description("The text to send.")]
            string? text = null)
    {
        var channelId = channel?.Id ?? Context.ChannelId;
        
        text ??= $"This message will be sent to {Mention.Channel(channelId)}.\n" +
                 "(This text will, too!)";

        var message = new LocalInteractionMessageResponse()
            .WithContent(text)
            .WithIsEphemeral(channelId == Context.ChannelId);

        var view = new SendMessageEditView(channelId, message);
        return Menu(new MessageEditMenu(view, Context.Interaction), TimeSpan.FromMinutes(30));
    }

    [SlashCommand("modify")]
    [Description("Modifies a message that was sent by the bot.")]
    public async Task<IResult> ModifyAsync(
        [Description("The channel the message was sent in.")]
        [ChannelTypes(ChannelType.Text, ChannelType.PrivateThread, ChannelType.PublicThread)]
        [RequireAuthorChannelPermissions(Permissions.SendMessages)]
        [RequireBotChannelPermissions(Permissions.SendMessages)]
            IChannel channel,
        [Description("The ID of the message.")]
            Snowflake messageId)
    {
        if (await Bot.FetchMessageAsync(channel.Id, messageId) is not IUserMessage { Author.IsBot: true } message ||
            message.Author.Id != Bot.CurrentUser.Id)
        {
            return Response($"No message sent by me could be found in {Mention.Channel(channel.Id)} with the ID {messageId}.").AsEphemeral();
        }

        var view = new ModifyMessageEditView(message);
        return Menu(new MessageEditMenu(view, Context.Interaction), TimeSpan.FromMinutes(30));
    }
}
using Disqord;
using Disqord.Bot.Commands;
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
    public async Task SendAsync(
        [Description("The channel to send the message to.")]
        [ChannelTypes(ChannelType.Text, ChannelType.PrivateThread, ChannelType.PublicThread)]
        [RequireAuthorChannelPermissions(Permissions.SendMessages)]
        [RequireBotChannelPermissions(Permissions.SendMessages)]
            IChannel channel,
        [Description("The text to send.")]
            string? text = null)
    {
        text ??= $"This message will be sent to {Mention.Channel(channel.Id)}.\n" +
                 "(This text will, too!)";

        var message = new LocalInteractionMessageResponse()
            .WithContent(text);

        var view = new SendMessageEditView(channel.Id, message);
        await Menu(new MessageEditMenu(view, Context.Interaction));
    }

    [SlashCommand("modify")]
    [Description("Modifies a message that was sent by the bot.")]
    public async Task ModifyAsync(
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
            await Response($"No message sent by me could be found in {Mention.Channel(channel.Id)} with the ID {messageId}.").AsEphemeral();
            return;
        }

        var view = new ModifyMessageEditView(message);
        await Menu(new MessageEditMenu(view, Context.Interaction));
    }
}
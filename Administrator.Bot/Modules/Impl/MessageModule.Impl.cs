using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Rest;
using Qmmands;

namespace Administrator.Bot;

public sealed partial class MessageModule : DiscordApplicationGuildModuleBase
{
    public partial IResult Send(IChannel? channel, string? text)
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

    public partial async Task<IResult> Modify(IChannel channel, Snowflake messageId)
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
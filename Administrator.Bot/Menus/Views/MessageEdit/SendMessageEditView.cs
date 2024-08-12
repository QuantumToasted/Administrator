using Disqord;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Rest;

namespace Administrator.Bot;

public sealed class SendMessageEditView : MessageEditView
{
    private readonly Snowflake _channelId;

    public SendMessageEditView(Snowflake channelId, LocalMessageBase message)
        : base(message)
    {
        _channelId = channelId;
    }

    public override async ValueTask SaveChangesAsync(ButtonEventArgs e)
    {
        var message = new LocalMessage {Content = Message.Content, Embeds = Message.Embeds};
        await Menu.Client.SendMessageAsync(_channelId, message);

        await e.Interaction.RespondOrFollowupAsync(new LocalInteractionMessageResponse()
            .WithContent("Message sent!")
            .WithIsEphemeral((Message as LocalInteractionMessageResponse)?.IsEphemeral ?? false));
        
        ClearComponents();
        await Menu.ApplyChangesAsync(e);
        Menu.Stop();
    }
}
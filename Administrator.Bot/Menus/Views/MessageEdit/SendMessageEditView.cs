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
        ClearComponents();
        await Menu.ApplyChangesAsync(e);
        Menu.Stop();

        await e.Interaction.Followup().SendAsync(new LocalInteractionMessageResponse()
            .WithContent("Message sent!"));
    }
}
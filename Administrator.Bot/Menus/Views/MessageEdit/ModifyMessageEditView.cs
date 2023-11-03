using Disqord;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Rest;
using Qommon;

namespace Administrator.Bot;

public sealed class ModifyMessageEditView : MessageEditView
{
    private readonly IUserMessage _message;

    public ModifyMessageEditView(IUserMessage message)
        : base(FormatMessage(message))
    {
        _message = message;
    }

    public override async ValueTask SaveChangesAsync(ButtonEventArgs e)
    {
        var message = new LocalMessage
        {
            Content = Message.Content,
            Embeds = Message.Embeds
        };

        await _message.ModifyAsync(x =>
        {
            x.Content = message.Content;
            x.Embeds = message.Embeds.GetValueOrDefault()?.ToList() ?? Optional<IEnumerable<LocalEmbed>>.Empty;
        });

        ClearComponents();
        await Menu.ApplyChangesAsync(e);
        Menu.Stop();

        await e.Interaction.Followup().SendAsync(new LocalInteractionMessageResponse()
            .WithContent("Message updated!"));
    }

    private static LocalMessageBase FormatMessage(IUserMessage message)
    {
        var localMessage = new LocalMessage();
        if (!string.IsNullOrWhiteSpace(message.Content))
            localMessage.WithContent(message.Content);

        if (message.Embeds.Count > 0)
            localMessage.WithEmbeds(message.Embeds.Select(LocalEmbed.CreateFrom));

        return localMessage;
    }
}
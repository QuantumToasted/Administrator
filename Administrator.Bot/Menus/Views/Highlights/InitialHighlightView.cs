using Disqord;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Gateway;

namespace Administrator.Bot;

public sealed class InitialHighlightView : ViewBase
{
    public InitialHighlightView(IGatewayUserMessage message, IMessageGuildChannel channel, Action<LocalMessageBase>? messageTemplate) 
        : base(messageTemplate)
    {
        AddComponent(new LinkButtonViewComponent(message.GetJumpUrl())
        {
            Label = "Jump to message"
        });
        
        AddComponent(new ButtonViewComponent(_ => Menu.SetViewAsync(new HighlightView(message, channel, null)))
        {
            Label = "Menu"
        });
    }
}
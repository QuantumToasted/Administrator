using Disqord;
using Disqord.Bot;
using Disqord.Extensions.Interactivity.Menus;

namespace Administrator.Bot;

public abstract class AdminViewBase : ViewBase
{
    protected AdminViewBase(Action<LocalMessageBase>? messageTemplate) 
        : base(messageTemplate)
    { }

    protected DiscordBotBase Bot => (DiscordBotBase)Menu.Client;
}
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Extensions.Interactivity.Menus;

namespace Administrator.Bot;

public abstract class GuildConfigurationViewBase(IDiscordApplicationGuildCommandContext context) 
    : ViewBase(null)
{
    private protected readonly IDiscordApplicationGuildCommandContext _context = context;

    // public DiscordBotBase Bot => _context.Bot;

    protected abstract string FormatContent();

    public override void FormatLocalMessage(LocalMessageBase message)
    {
        message.WithContent(FormatContent());
        base.FormatLocalMessage(message);
    }

    [Button(Label = "Return to Main Menu", Row = 4, Position = 4, Style = LocalButtonComponentStyle.Danger)]
    public ValueTask MainMenuAsync(ButtonEventArgs e)
        => Menu.SetViewAsync(new GuildConfigurationMainMenuView(_context));
}
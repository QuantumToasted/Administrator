using Disqord.Bot.Commands.Application;

namespace Administrator.Bot;

public sealed class PunishmentMessageConfigurationView(IDiscordApplicationGuildCommandContext context) 
    : GuildConfigurationViewBase(context)
{
    public const string SELECTION_TEXT = "Custom Punishment Message";

    protected override string FormatContent()
        => SELECTION_TEXT;
}
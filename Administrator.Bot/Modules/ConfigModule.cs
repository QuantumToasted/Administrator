using Disqord;
using Disqord.Bot.Commands.Application;
using Qmmands;
using IResult = Qmmands.IResult;

namespace Administrator.Bot;

public sealed class ConfigModule : DiscordApplicationGuildModuleBase
{
    [SlashCommand("config")]
    [Description("Configures various bot settings and features.")]
    [RequireInitialAuthorPermissions(Permissions.ManageGuild)]
    public IResult ConfigMenu()
        => View(new GuildConfigurationMainMenuView(Context));
}
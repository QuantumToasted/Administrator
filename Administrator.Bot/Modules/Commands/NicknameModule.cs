using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("nickname")]
[RequireInitialAuthorPermissions(Permissions.SetNick)]
[RequireBotPermissions(Permissions.ManageNicks)]
public sealed partial class NicknameModule : DiscordApplicationGuildModuleBase
{
    [SlashCommand("set")]
    [Description("Sets your nickname.")]
    public partial Task<IResult> Set(
        [Description("Your new nickname.")]
        [Maximum(32)]
            string nickname);

    [SlashCommand("remove")]
    [Description("Removes your current nickname, if any.")]
    public partial Task<IResult> Remove();
}
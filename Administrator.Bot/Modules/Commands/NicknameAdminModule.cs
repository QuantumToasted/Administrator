using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("nickname-admin")]
[RequireInitialAuthorPermissions(Permissions.ManageNicks)]
[RequireBotPermissions(Permissions.ManageNicks)]
public sealed partial class NicknameAdminModule
{
    [SlashCommand("set")]
    [Description("Sets another user's nickname.")]
    public partial Task<IResult> Set(
        [Description("The member whose nickname is being set.")]
            IMember member,
        [Description("The new nickname for the member.")]
        [Maximum(32)]
            string nickname);

    [SlashCommand("remove")]
    [Description("Removes another user's nickname.")]
    public partial Task<IResult> Remove(
        [Description("The member whose nickname is being removed.")]
            IMember member);
}
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Rest;
using Qmmands;
using IResult = Qmmands.IResult;

namespace Administrator.Bot;

[SlashGroup("nickname-admin")]
[RequireInitialAuthorPermissions(Permissions.ManageNicks)]
[RequireBotPermissions(Permissions.ManageNicks)]
public sealed class NicknameAdminModule : DiscordApplicationGuildModuleBase
{
    [SlashCommand("set")]
    public async Task<IResult> SetAsync(
        [Description("The member whose nickname is being set.")]
            IMember member, 
        [Description("The new nickname for the member.")]
        [Maximum(32)] 
            string nickname)
    {
        await member.ModifyAsync(x => x.Nick = nickname);
        return Response($"{Markdown.Bold(member.Tag)}'s nickname has been updated to \"{Markdown.Bold(Markdown.Escape(nickname))}\".");
    }
    
    [SlashCommand("remove")]
    public async Task<IResult> RemoveAsync(
        [Description("The member whose nickname is being removed.")]
            IMember member)
    {
        await member.ModifyAsync(x => x.Nick = string.Empty);
        return Response($"{Markdown.Bold(member.Tag)}'s nickname has been cleared.");
    }
}
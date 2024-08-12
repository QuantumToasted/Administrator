using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Http;
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
    [Description("Sets another user's nickname.")]
    public async Task<IResult> SetAsync(
        [Description("The member whose nickname is being set.")]
            IMember member, 
        [Description("The new nickname for the member.")]
        [Maximum(32)] 
            string nickname)
    {
        try
        {
            await member.ModifyAsync(x => x.Nick = nickname);
        }
        catch (RestApiException ex) when (ex.StatusCode == HttpResponseStatusCode.Forbidden)
        {
            return Response($"Unfortunately, I was unable to set {member.Mention}'s nickname. " +
                            "This may be due to them being higher than me in the server role hierarchy.").AsEphemeral();
        }
        
        return Response($"{Markdown.Bold(member.Tag)}'s nickname has been updated to \"{Markdown.Bold(Markdown.Escape(nickname))}\".");
    }
    
    [SlashCommand("remove")]
    [Description("Removes another user's nickname.")]
    public async Task<IResult> RemoveAsync(
        [Description("The member whose nickname is being removed.")]
            IMember member)
    {
        try
        {
            await member.ModifyAsync(x => x.Nick = string.Empty);
        }
        catch (RestApiException ex) when (ex.StatusCode == HttpResponseStatusCode.Forbidden)
        {
            return Response($"Unfortunately, I was unable to remove {member.Mention}'s nickname. " +
                            "This may be due to them being higher than me in the server role hierarchy.").AsEphemeral();
        }
        
        return Response($"{Markdown.Bold(member.Tag)}'s nickname has been cleared.");
    }
}
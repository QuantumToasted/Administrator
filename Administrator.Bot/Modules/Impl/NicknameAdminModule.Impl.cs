using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Http;
using Disqord.Rest;
using Qmmands;

namespace Administrator.Bot;

public sealed partial class NicknameAdminModule : DiscordApplicationGuildModuleBase
{
    public partial async Task<IResult> Set(IMember member, string nickname)
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
    
    public partial async Task<IResult> Remove(IMember member)
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
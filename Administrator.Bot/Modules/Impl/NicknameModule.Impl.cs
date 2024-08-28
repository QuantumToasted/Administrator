using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Http;
using Disqord.Rest;
using Qmmands;

namespace Administrator.Bot;

public sealed partial class NicknameModule : DiscordApplicationGuildModuleBase
{
    public partial async Task<IResult> Set(string nickname)
    {
        try
        {
            await Context.Author.ModifyAsync(x => x.Nick = nickname);
        }
        catch (RestApiException ex) when (ex.StatusCode == HttpResponseStatusCode.Forbidden)
        {
            return Response("Unfortunately, I was unable to set your nickname. " +
                            "This may be due to you being higher than me in the server role hierarchy.").AsEphemeral();
        }
        
        return Response($"Nickname updated! Your new nickname is \"{Markdown.Bold(Markdown.Escape(nickname))}\".");
    }
    
    public partial async Task<IResult> Remove()
    {
        try
        {
            await Context.Author.ModifyAsync(x => x.Nick = string.Empty);
        }
        catch (RestApiException ex) when (ex.StatusCode == HttpResponseStatusCode.Forbidden)
        {
            return Response("Unfortunately, I was unable to remove your nickname. " +
                            "This may be due to you being higher than me in the server role hierarchy.").AsEphemeral();
        }
        
        return Response("Nickname cleared.");
    }
}
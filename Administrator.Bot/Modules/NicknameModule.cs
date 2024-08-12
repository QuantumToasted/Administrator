using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Http;
using Disqord.Rest;
using Qmmands;
using IResult = Qmmands.IResult;

namespace Administrator.Bot;

[SlashGroup("nickname")]
[RequireInitialAuthorPermissions(Permissions.SetNick)]
[RequireBotPermissions(Permissions.ManageNicks)]
public sealed class NicknameModule : DiscordApplicationGuildModuleBase
{
    [SlashCommand("set")]
    [Description("Sets your nickname.")]
    public async Task<IResult> SetAsync(
        [Description("Your new nickname.")]
        [Maximum(32)] 
            string nickname)
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
    
    [SlashCommand("remove")]
    [Description("Removes your current nickname, if any.")]
    public async Task<IResult> RemoveAsync()
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
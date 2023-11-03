using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
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
    public async Task<IResult> SetAsync(
        [Description("Your new nickname.")]
        [Maximum(32)] 
            string nickname)
    {
        await Context.Author.ModifyAsync(x => x.Nick = nickname);
        return Response($"Nickname updated! Your new nickname is \"{Markdown.Bold(Markdown.Escape(nickname))}\".");
    }
    
    [SlashCommand("remove")]
    public async Task<IResult> RemoveAsync()
    {
        await Context.Author.ModifyAsync(x => x.Nick = string.Empty);
        return Response("Nickname cleared.");
    }
}
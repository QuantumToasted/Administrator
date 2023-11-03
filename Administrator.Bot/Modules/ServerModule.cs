using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Extensions.Interactivity.Menus.Prompt;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("server")]
[RequireInitialAuthorPermissions(Permissions.ManageGuild)]
public sealed class ServerModule(AdminDbContext db) : DiscordApplicationGuildModuleBase
{
    [SlashCommand("reset-xp")]
    [Description("Resets everyone's XP to 0 in this server.")]
    public async Task<IResult> ResetXpAsync()
    {
        var guildUsers = await db.GuildUsers.Where(x => x.GuildId == Context.GuildId && x.TotalXp > 0).ToListAsync();

        var prompt = new PromptView(x =>
            x.WithContent($"This action will reset the XP of {"member".ToQuantity(guildUsers.Count)} to 0.\n" +
                          $"It will not assign or remove any existing level rewards."));

        await View(prompt);

        if (!prompt.Result)
            return Response("Action canceled or timed out.");

        guildUsers.ForEach(x =>
        {
            x.TotalXp = 0;
            x.LastXpGain = DateTimeOffset.UtcNow;
        });
        
        await db.SaveChangesAsync();
        return Response("XP stats have been reset for this server.");
    }
}
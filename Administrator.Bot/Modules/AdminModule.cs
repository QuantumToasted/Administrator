using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("admin")]
[RequireInitialAuthorPermissions(Permissions.Administrator)]
public sealed class AdminModule(AdminDbContext db) : DiscordApplicationGuildModuleBase
{
    [SlashCommand("api-key")]
    [Description("Generates a new key for third-party API access.")]
    public async Task GenerateApiKeyAsync()
    {
        var guild = await db.Guilds.GetOrCreateAsync(Context.GuildId);
        var apiKey = guild.RegenerateApiKey();
        var view = new AdminPromptView("A new API key will be generated, invalidating any previous API keys generated.", isEphemeral: true)
            .OnConfirm($"Your new API key (don't share this with anyone else!)\n{Markdown.CodeBlock(apiKey)}");

        await View(view);

        if (view.Result)
        {
            await db.SaveChangesAsync();
        }
    }
    
    /*
    [SlashCommand("reset-xp")]
    [Description("Resets all server members' XP to 0 in this server. Only affects members who have gained XP.")]
    public async Task ResetXpAsync()
    {
        var guildUserCount = await EntityFrameworkQueryableExtensions.CountAsync(db.Members, x => x.GuildId == Context.GuildId && x.TotalXp > 0);
        var view = new AdminPromptView($"This action will reset the XP of {Markdown.Bold("member".ToQuantity(guildUserCount))} to {Markdown.Bold(0)}.\n" +
                                       $"It will not assign or remove any existing level rewards.")
            .OnConfirm("XP stats have been reset for this server.");

        await View(view);
        if (view.Result)
        {
            await db.Members.Where(x => x.GuildId == Context.GuildId && x.TotalXp > 0)
                .Set(x => x.TotalXp, x => 0)
                .Set(x => x.LastXpGain, x => DateTimeOffset.UtcNow)
                .UpdateAsync();
        }
    }
    */
}
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;

namespace Administrator.Bot;

public sealed partial class AdminModule(AdminDbContext db) : DiscordApplicationGuildModuleBase
{
    public partial async Task GenerateApiKey()
    {
        var guild = await db.Guilds.GetOrCreateAsync(Context.GuildId);
        guild.ApiKey = Guid.NewGuid().ToString("N");
        
        var view = new AdminPromptView("A new API key will be generated, invalidating any previous API keys generated.", isEphemeral: true)
            .OnConfirm($"Your new API key (don't share this with anyone else!)\n{Markdown.CodeBlock(guild.ApiKey)}");

        await View(view);

        if (view.Result)
        {
            await db.SaveChangesAsync();
        }
    }
}
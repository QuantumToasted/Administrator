using Administrator.Database;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using LinqToDB;
using Microsoft.Extensions.Logging;

namespace Administrator.Bot;

public sealed class EventDatabaseService : DiscordBotService
{
    protected override ValueTask OnJoinedGuild(JoinedGuildEventArgs e)
        => InsertNewGuild(e.Guild);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Bot.WaitUntilReadyAsync(stoppingToken);
        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);

        foreach (var chunk in Bot.GetGuilds().Keys.Chunk(50))
        {
            var guilds = chunk.Select(GuildConfiguration.Create);
            var res = await db.Guilds.Merge()
                .Using(guilds)
                .OnTargetKey()
                .InsertWhenNotMatched()
                .MergeAsync(stoppingToken);
            
            if (res > 0)
                Logger.LogDebug("Inserted {Count} new guilds.", res);
        }
    }

    private async ValueTask InsertNewGuild(IGuild guild)
    {
        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);

        try
        {
            var newGuild = GuildConfiguration.Create(guild);
            var res = await db.Guilds.Merge()
                .Using([newGuild])
                .OnTargetKey()
                .InsertWhenNotMatched()
                .MergeAsync();
            
            if (res > 0)
            {
                Logger.LogDebug("Inserted new guild {GuildId}.", guild.Id.RawValue);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to merge available guild into database.");
        }
    }
}